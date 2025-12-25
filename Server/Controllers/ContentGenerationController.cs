using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Services;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;
using System.Security.Claims;

namespace StudieAssistenten.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ContentGenerationController : BaseApiController
{
    private readonly IAiContentGenerationService _aiService;
    private readonly ApplicationDbContext _context;
    private readonly IFlashcardPdfGenerationService _flashcardPdfService;
    private readonly IPracticeTestPdfGenerationService _practiceTestPdfService;
    private readonly ISummaryPdfGenerationService _summaryPdfService;
    private readonly ILogger<ContentGenerationController> _logger;

    public ContentGenerationController(
        IAiContentGenerationService aiService,
        ApplicationDbContext context,
        IFlashcardPdfGenerationService flashcardPdfService,
        IPracticeTestPdfGenerationService practiceTestPdfService,
        ISummaryPdfGenerationService summaryPdfService,
        ILogger<ContentGenerationController> logger)
    {
        _aiService = aiService;
        _context = context;
        _flashcardPdfService = flashcardPdfService;
        _practiceTestPdfService = practiceTestPdfService;
        _summaryPdfService = summaryPdfService;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateContent([FromBody] GenerateContentRequestDto request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Get document and verify ownership via Test relationship
            var document = await _context.StudyDocuments
                .Include(d => d.Test)
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId);

            if (document == null || document.Test == null || document.Test.UserId != userId)
            {
                return NotFound(new { message = "Document not found" });
            }

            if (string.IsNullOrWhiteSpace(document.ExtractedText))
            {
                return BadRequest(new { message = "Document has no extracted text. Please run OCR first." });
            }

            // Generate content based on type
            var generatedContent = request.ProcessingType switch
            {
                ProcessingType.Flashcards => await _aiService.GenerateFlashcardsAsync(request.DocumentId, request.TeacherInstructions),
                ProcessingType.PracticeTest => await _aiService.GeneratePracticeTestAsync(request.DocumentId, request.TeacherInstructions),
                ProcessingType.Summary => await _aiService.GenerateSummaryAsync(request.DocumentId, request.TeacherInstructions),
                _ => throw new InvalidOperationException($"Unsupported processing type: {request.ProcessingType}")
            };

            return Ok(MapToDto(generatedContent));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation during content generation");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content for document {DocumentId}", request.DocumentId);
            return StatusCode(500, new { message = "An error occurred while generating content" });
        }
    }

    [HttpGet("document/{documentId}")]
    public async Task<IActionResult> GetGeneratedContent(int documentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Verify document ownership via Test relationship
        var document = await _context.StudyDocuments
            .Include(d => d.Test)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null || document.Test == null || document.Test.UserId != userId)
        {
            return NotFound();
        }

        var contents = await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Where(gc => gc.StudyDocumentId == documentId)
            .OrderByDescending(gc => gc.GeneratedAt)
            .ToListAsync();

        var result = contents.Select(MapToDto).ToList();
        return Ok(result);
    }

    [HttpGet("test/{testId}")]
    public async Task<IActionResult> GetTestGeneratedContent(int testId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Verify test ownership
        var test = await _context.Tests.FirstOrDefaultAsync(t => t.Id == testId && t.UserId == userId);
        if (test == null)
        {
            return NotFound();
        }

        // Get all documents for this test
        var documentIds = await _context.StudyDocuments
            .Where(d => d.TestId == testId)
            .Select(d => d.Id)
            .ToListAsync();

        if (!documentIds.Any())
        {
            return Ok(new List<object>());
        }

        // Get all generated content for those documents
        var contents = await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Where(gc => documentIds.Contains(gc.StudyDocumentId))
            .OrderByDescending(gc => gc.GeneratedAt)
            .ToListAsync();

        var result = contents.Select(MapToDto).ToList();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetContent(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Get content and verify ownership via Document -> Test relationship
        var content = await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Include(gc => gc.StudyDocument)
                .ThenInclude(d => d!.Test)
            .FirstOrDefaultAsync(gc => gc.Id == id);

        if (content == null || content.StudyDocument?.Test?.UserId != userId)
        {
            return NotFound();
        }

        return Ok(MapToDto(content));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContent(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Get content and verify ownership via Document -> Test relationship
        var content = await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Include(gc => gc.StudyDocument)
                .ThenInclude(d => d!.Test)
            .FirstOrDefaultAsync(gc => gc.Id == id);

        if (content == null || content.StudyDocument?.Test?.UserId != userId)
        {
            return NotFound();
        }

        // content is guaranteed non-null here due to check above
        _context.GeneratedContents.Remove(content);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted generated content {ContentId}", id);

        return NoContent();
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Get content and verify ownership via Document -> Test relationship
        var content = await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Include(gc => gc.StudyDocument)
                .ThenInclude(d => d!.Test)
            .FirstOrDefaultAsync(gc => gc.Id == id);

        if (content == null || content.StudyDocument?.Test?.UserId != userId)
        {
            return NotFound();
        }

        try
        {
            byte[] pdfBytes;
            string fileName;

            switch (content.ProcessingType)
            {
                case ProcessingType.Flashcards:
                    if (content.Flashcards == null || !content.Flashcards.Any())
                    {
                        return BadRequest(new { message = "No flashcards found for this content" });
                    }
                    pdfBytes = _flashcardPdfService.GenerateFlashcardPdf(content);
                    fileName = $"flashcards_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    break;

                case ProcessingType.PracticeTest:
                    pdfBytes = _practiceTestPdfService.GeneratePracticeTestPdf(content);
                    fileName = $"practice_test_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    break;

                case ProcessingType.Summary:
                    pdfBytes = _summaryPdfService.GenerateSummaryPdf(content);
                    fileName = $"summary_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    break;

                default:
                    return BadRequest(new { message = $"PDF generation not supported for {content.ProcessingType}" });
            }

            _logger.LogInformation("Generated {Type} PDF for content {ContentId}", content.ProcessingType, id);

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for content {ContentId}", id);
            return StatusCode(500, new { message = "Error generating PDF" });
        }
    }

    private static GeneratedContentDto MapToDto(GeneratedContent content)
    {
        return new GeneratedContentDto
        {
            Id = content.Id,
            Title = content.Title,
            ProcessingType = content.ProcessingType,
            GeneratedAt = content.GeneratedAt,
            Content = content.Content ?? string.Empty,
            FlashcardsCount = content.Flashcards?.Count ?? 0,
            Flashcards = content.Flashcards?.OrderBy(f => f.Order).Select(f => new FlashcardDto
            {
                Id = f.Id,
                Question = f.Question,
                Answer = f.Answer,
                Order = f.Order
            }).ToList() ?? new List<FlashcardDto>()
        };
    }
}
