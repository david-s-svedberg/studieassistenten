using Microsoft.AspNetCore.Mvc;
using StudieAssistenten.Server.Services;

namespace StudieAssistenten.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcessingController : ControllerBase
{
    private readonly IDocumentProcessingService _processingService;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<ProcessingController> _logger;

    public ProcessingController(
        IDocumentProcessingService processingService,
        IFileUploadService fileUploadService,
        ILogger<ProcessingController> logger)
    {
        _processingService = processingService;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Trigger OCR/text extraction for a document
    /// </summary>
    [HttpPost("{documentId}/extract")]
    public async Task<IActionResult> ExtractText(int documentId)
    {
        try
        {
            // Start processing in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await _processingService.ProcessDocumentAsync(documentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background processing failed for document {DocumentId}", documentId);
                }
            });

            return Accepted(new { message = "Processing started", documentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting processing for document {DocumentId}", documentId);
            return StatusCode(500, "An error occurred while starting document processing");
        }
    }

    /// <summary>
    /// Update the extracted text for a document (manual editing)
    /// </summary>
    [HttpPut("{documentId}/text")]
    public async Task<IActionResult> UpdateExtractedText(int documentId, [FromBody] UpdateTextRequest request)
    {
        try
        {
            var result = await _fileUploadService.UpdateExtractedTextAsync(documentId, request.Text);
            if (!result)
            {
                return NotFound();
            }

            return Ok(new { message = "Text updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating text for document {DocumentId}", documentId);
            return StatusCode(500, "An error occurred while updating the text");
        }
    }
}

public class UpdateTextRequest
{
    public string Text { get; set; } = string.Empty;
}
