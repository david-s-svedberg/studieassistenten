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

    /// <summary>
    /// Generates AI-powered study content (flashcards, practice tests, or summaries) from test documents
    /// </summary>
    /// <param name="request">Content generation parameters including test ID and content type</param>
    /// <returns>The generated content as a DTO with PDF and metadata</returns>
    /// <response code="200">Content successfully generated</response>
    /// <response code="400">Invalid request (missing documents or unprocessed documents)</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Test not found or user doesn't own the test</response>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(GeneratedContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateContent([FromBody] GenerateContentRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

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

            var ownershipCheck = VerifyTestOwnership(test, userId);
            if (ownershipCheck != null)
            {
                return ownershipCheck;
            }

            if (!test!.Documents.Any())
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
                ProcessingType.Flashcards => await _aiService.GenerateFlashcardsAsync(request),
                ProcessingType.PracticeTest => await _aiService.GeneratePracticeTestAsync(request),
                ProcessingType.Summary => await _aiService.GenerateSummaryAsync(request),
                _ => throw new InvalidOperationException($"Unsupported processing type: {request.ProcessingType}")
            };

            return Ok(_mapper.Map<GeneratedContentDto>(generatedContent));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Invalid operation during content generation");
            return BadRequestError(ex.Message, errorCode: "INVALID_OPERATION");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating content for test {TestId}", request.TestId);
            return InternalServerError("An error occurred while generating content", errorCode: "CONTENT_GENERATION_ERROR");
        }
    }

    /// <summary>
    /// Gets all generated content for a specific document
    /// </summary>
    /// <param name="documentId">The ID of the document</param>
    /// <returns>List of generated content for the document</returns>
    /// <response code="200">Returns list of generated content</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Document not found or user doesn't own it</response>
    [HttpGet("document/{documentId}")]
    [ProducesResponseType(typeof(List<GeneratedContentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        var ownershipCheck = VerifyDocumentOwnership(document, userId);
        if (ownershipCheck != null) return ownershipCheck;

        var contents = await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Where(gc => gc.StudyDocumentId == documentId)
            .OrderByDescending(gc => gc.GeneratedAt)
            .ToListAsync();

        var result = _mapper.Map<List<GeneratedContentDto>>(contents);
        return Ok(result);
    }

    /// <summary>
    /// Gets all generated content for a specific test
    /// </summary>
    /// <param name="testId">The ID of the test</param>
    /// <returns>List of all generated content (flashcards, tests, summaries) for the test</returns>
    /// <response code="200">Returns list of generated content</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Test not found or user doesn't own it</response>
    [HttpGet("test/{testId}")]
    [ProducesResponseType(typeof(List<GeneratedContentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTestGeneratedContent(int testId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Verify test ownership
        var test = await _context.Tests.FirstOrDefaultAsync(t => t.Id == testId);
        var ownershipCheck = VerifyTestOwnership(test, userId);
        if (ownershipCheck != null) return ownershipCheck;

        // Get all generated content for this test
        var contents = await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Where(gc => gc.TestId == testId)
            .OrderByDescending(gc => gc.GeneratedAt)
            .ToListAsync();

        var result = _mapper.Map<List<GeneratedContentDto>>(contents);
        return Ok(result);
    }

    /// <summary>
    /// Gets generated content for a specific test with pagination
    /// </summary>
    /// <param name="testId">The ID of the test</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page (1-100)</param>
    /// <returns>Paginated list of generated content</returns>
    /// <response code="200">Returns paginated generated content</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Test not found or user doesn't own it</response>
    [HttpGet("test/{testId}/paged")]
    [ProducesResponseType(typeof(PagedResultDto<GeneratedContentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTestGeneratedContentPaged(
        int testId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        // Validate pagination parameters
        if (pageNumber < 1)
        {
            return BadRequestError("Page number must be at least 1");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequestError("Page size must be between 1 and 100");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Verify test ownership
        var test = await _context.Tests.FirstOrDefaultAsync(t => t.Id == testId);
        var ownershipCheck = VerifyTestOwnership(test, userId);
        if (ownershipCheck != null) return ownershipCheck;

        // Get total count
        var totalCount = await _context.GeneratedContents
            .Where(gc => gc.TestId == testId)
            .CountAsync();

        // Get paginated content
        var contents = await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Where(gc => gc.TestId == testId)
            .OrderByDescending(gc => gc.GeneratedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = _mapper.Map<List<GeneratedContentDto>>(contents);
        var result = new PagedResultDto<GeneratedContentDto>(items, totalCount, pageNumber, pageSize);

        return Ok(result);
    }

    /// <summary>
    /// Gets a specific generated content item by ID
    /// </summary>
    /// <param name="id">The ID of the generated content</param>
    /// <returns>The generated content details</returns>
    /// <response code="200">Returns the generated content</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Content not found or user doesn't own it</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GeneratedContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        var ownershipCheck = VerifyContentOwnership(content, userId);
        if (ownershipCheck != null) return ownershipCheck;

        return Ok(_mapper.Map<GeneratedContentDto>(content));
    }

    /// <summary>
    /// Deletes a generated content item
    /// </summary>
    /// <param name="id">The ID of the generated content to delete</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Content successfully deleted</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Content not found or user doesn't own it</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        var ownershipCheck = VerifyContentOwnership(content, userId);
        if (ownershipCheck != null) return ownershipCheck;

        // content is guaranteed non-null here due to check above
        _context.GeneratedContents.Remove(content!);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted generated content {ContentId}", id);

        return NoContent();
    }

    /// <summary>
    /// Downloads the PDF for a generated content item
    /// </summary>
    /// <param name="id">The ID of the generated content</param>
    /// <returns>PDF file as binary content</returns>
    /// <response code="200">Returns the PDF file</response>
    /// <response code="400">Invalid content type or missing flashcards</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="404">Content not found or user doesn't own it</response>
    /// <response code="500">Error generating PDF</response>
    [HttpGet("{id}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
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

        var ownershipCheck = VerifyContentOwnership(content, userId);
        if (ownershipCheck != null) return ownershipCheck;

        try
        {
            byte[] pdfBytes;
            string fileName;

            switch (content!.ProcessingType)
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
            return InternalServerError("Error generating PDF", errorCode: "PDF_GENERATION_ERROR");
        }
    }

}
