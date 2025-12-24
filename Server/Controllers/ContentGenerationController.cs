using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Services;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;

namespace StudieAssistenten.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentGenerationController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ContentGenerationController> _logger;

    public ContentGenerationController(
        IServiceProvider serviceProvider,
        ILogger<ContentGenerationController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateContent([FromBody] GenerateContentRequestDto request)
    {
        try
        {
            // Create a new scope for background processing
            using var scope = _serviceProvider.CreateScope();
            var aiService = scope.ServiceProvider.GetRequiredService<IAiContentGenerationService>();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Update document status
            var document = await context.StudyDocuments.FindAsync(request.DocumentId);
            if (document == null)
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
                ProcessingType.Flashcards => await aiService.GenerateFlashcardsAsync(request.DocumentId, request.TeacherInstructions),
                ProcessingType.PracticeTest => await aiService.GeneratePracticeTestAsync(request.DocumentId, request.TeacherInstructions),
                ProcessingType.Summary => await aiService.GenerateSummaryAsync(request.DocumentId, request.TeacherInstructions),
                _ => throw new InvalidOperationException($"Unsupported processing type: {request.ProcessingType}")
            };

            return Ok(new
            {
                generatedContent.Id,
                generatedContent.Title,
                generatedContent.ProcessingType,
                generatedContent.GeneratedAt,
                generatedContent.Content,
                FlashcardsCount = generatedContent.Flashcards.Count
            });
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
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var contents = await context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Where(gc => gc.StudyDocumentId == documentId)
            .OrderByDescending(gc => gc.GeneratedAt)
            .ToListAsync();

        var result = contents.Select(gc => new
        {
            gc.Id,
            gc.Title,
            gc.ProcessingType,
            gc.GeneratedAt,
            gc.Content,
            FlashcardsCount = gc.Flashcards.Count,
            Flashcards = gc.Flashcards.OrderBy(f => f.Order).Select(f => new
            {
                f.Id,
                f.Question,
                f.Answer,
                f.Order
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    [HttpGet("test/{testId}")]
    public async Task<IActionResult> GetTestGeneratedContent(int testId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Get all documents for this test
        var documentIds = await context.StudyDocuments
            .Where(d => d.TestId == testId)
            .Select(d => d.Id)
            .ToListAsync();

        if (!documentIds.Any())
        {
            return Ok(new List<object>());
        }

        // Get all generated content for those documents
        var contents = await context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Where(gc => documentIds.Contains(gc.StudyDocumentId))
            .OrderByDescending(gc => gc.GeneratedAt)
            .ToListAsync();

        var result = contents.Select(gc => new
        {
            gc.Id,
            gc.Title,
            gc.ProcessingType,
            gc.GeneratedAt,
            gc.Content,
            FlashcardsCount = gc.Flashcards.Count,
            Flashcards = gc.Flashcards.OrderBy(f => f.Order).Select(f => new
            {
                f.Id,
                f.Question,
                f.Answer,
                f.Order
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetContent(int id)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var content = await context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .FirstOrDefaultAsync(gc => gc.Id == id);

        if (content == null)
        {
            return NotFound();
        }

        var result = new
        {
            content.Id,
            content.Title,
            content.ProcessingType,
            content.GeneratedAt,
            content.Content,
            FlashcardsCount = content.Flashcards.Count,
            Flashcards = content.Flashcards.OrderBy(f => f.Order).Select(f => new
            {
                f.Id,
                f.Question,
                f.Answer,
                f.Order
            }).ToList()
        };

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteContent(int id)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var content = await context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .FirstOrDefaultAsync(gc => gc.Id == id);

        if (content == null)
        {
            return NotFound();
        }

        context.GeneratedContents.Remove(content);
        await context.SaveChangesAsync();

        _logger.LogInformation("Deleted generated content {ContentId}", id);

        return NoContent();
    }

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var content = await context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .FirstOrDefaultAsync(gc => gc.Id == id);

        if (content == null)
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
                    var flashcardService = scope.ServiceProvider.GetRequiredService<IFlashcardPdfGenerationService>();
                    pdfBytes = flashcardService.GenerateFlashcardPdf(content);
                    fileName = $"flashcards_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    break;

                case ProcessingType.PracticeTest:
                    var practiceTestService = scope.ServiceProvider.GetRequiredService<IPracticeTestPdfGenerationService>();
                    pdfBytes = practiceTestService.GeneratePracticeTestPdf(content);
                    fileName = $"practice_test_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    break;

                case ProcessingType.Summary:
                    var summaryService = scope.ServiceProvider.GetRequiredService<ISummaryPdfGenerationService>();
                    pdfBytes = summaryService.GenerateSummaryPdf(content);
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
