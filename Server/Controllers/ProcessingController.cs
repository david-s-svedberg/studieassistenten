using Microsoft.AspNetCore.Mvc;
using StudieAssistenten.Server.Services;

namespace StudieAssistenten.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcessingController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessingController> _logger;

    public ProcessingController(
        IServiceProvider serviceProvider,
        ILogger<ProcessingController> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Trigger OCR/text extraction for a document
    /// </summary>
    [HttpPost("{documentId}/extract")]
    public IActionResult ExtractText(int documentId)
    {
        try
        {
            // Start processing in background with a new scope
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var processingService = scope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();
                    await processingService.ProcessDocumentAsync(documentId);
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
            using var scope = _serviceProvider.CreateScope();
            var fileUploadService = scope.ServiceProvider.GetRequiredService<IFileUploadService>();
            var result = await fileUploadService.UpdateExtractedTextAsync(documentId, request.Text);
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
