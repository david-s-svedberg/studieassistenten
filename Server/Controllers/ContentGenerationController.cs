using AutoMapper;
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
    private readonly IMapper _mapper;
    private readonly ILogger<ContentGenerationController> _logger;

    public ContentGenerationController(
        IAiContentGenerationService aiService,
        ApplicationDbContext context,
        IFlashcardPdfGenerationService flashcardPdfService,
        IPracticeTestPdfGenerationService practiceTestPdfService,
        ISummaryPdfGenerationService summaryPdfService,
        IMapper mapper,
        ILogger<ContentGenerationController> logger)
    {
        _aiService = aiService;
        _context = context;
        _flashcardPdfService = flashcardPdfService;
        _practiceTestPdfService = practiceTestPdfService;
        _summaryPdfService = summaryPdfService;
        _mapper = mapper;
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

            // Get test and verify ownership
            var test = await _context.Tests
                .Include(t => t.Documents)
                .FirstOrDefaultAsync(t => t.Id == request.TestId);

            if (test == null || test.UserId != userId)
            {
                return NotFound(new { message = "Test not found" });
            }

            if (!test.Documents.Any())
            {
                return BadRequest(new { message = "Test has no documents. Please upload documents first." });
            }

            if (!test.Documents.Any(d => !string.IsNullOrWhiteSpace(d.ExtractedText)))
            {
                return BadRequest(new { message = "No documents have extracted text. Please wait for OCR processing to complete." });
            }

            // Generate content based on type (using all documents in the test)
            var generatedContent = request.ProcessingType switch
            {
                ProcessingType.Flashcards => await _aiService.GenerateFlashcardsAsync(request.TestId, request.TeacherInstructions),
                ProcessingType.PracticeTest => await _aiService.GeneratePracticeTestAsync(request.TestId, request.TeacherInstructions),
                ProcessingType.Summary => await _aiService.GenerateSummaryAsync(request.TestId, request.TeacherInstructions),
                _ => throw new InvalidOperationException($"Unsupported processing type: {request.ProcessingType}")
            };

            return Ok(_mapper.Map<GeneratedContentDto>(generatedContent));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation during content generation");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content for test {TestId}", request.TestId);
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

        var result = _mapper.Map<List<GeneratedContentDto>>(contents);
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

        // Get all generated content for this test
        var contents = await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Where(gc => gc.TestId == testId)
            .OrderByDescending(gc => gc.GeneratedAt)
            .ToListAsync();

        var result = _mapper.Map<List<GeneratedContentDto>>(contents);
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

        // Get content and verify ownership via Test relationship
        var content = await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Include(gc => gc.Test)
            .Include(gc => gc.StudyDocument) // Legacy: for backward compatibility
                .ThenInclude(d => d!.Test)
            .FirstOrDefaultAsync(gc => gc.Id == id);

        if (content == null || content.Test?.UserId != userId)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<GeneratedContentDto>(content));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContent(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Get content and verify ownership via Test relationship
        var content = await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Include(gc => gc.Test)
            .Include(gc => gc.StudyDocument) // Legacy: for backward compatibility
                .ThenInclude(d => d!.Test)
            .FirstOrDefaultAsync(gc => gc.Id == id);

        if (content == null || content.Test?.UserId != userId)
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

        // Get content and verify ownership via Test relationship
        var content = await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Include(gc => gc.Test)
            .Include(gc => gc.StudyDocument) // Legacy: for backward compatibility
                .ThenInclude(d => d!.Test)
            .FirstOrDefaultAsync(gc => gc.Id == id);

        if (content == null || content.Test?.UserId != userId)
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

}
