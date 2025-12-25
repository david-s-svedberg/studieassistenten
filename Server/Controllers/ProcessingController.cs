using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace StudieAssistenten.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProcessingController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessingController> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public ProcessingController(
        IServiceProvider serviceProvider,
        ILogger<ProcessingController> logger,
        IHostApplicationLifetime lifetime)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _lifetime = lifetime;
    }

    /// <summary>
    /// Trigger OCR/text extraction for a document
    /// </summary>
    [HttpPost("{documentId}/extract")]
    public async Task<IActionResult> ExtractText(int documentId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Verify document ownership via Test relationship
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var document = await context.StudyDocuments
                .Include(d => d.Test)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null || document.Test == null || document.Test.UserId != userId)
            {
                return NotFound();
            }

            // Start processing in background with cancellation token support
            var cancellationToken = _lifetime.ApplicationStopping;

            _ = Task.Run(async () =>
            {
                try
                {
                    // Check if cancellation was requested before starting
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Processing cancelled before start for document {DocumentId}", documentId);
                        return;
                    }

                    using var processingScope = _serviceProvider.CreateScope();
                    var processingService = processingScope.ServiceProvider.GetRequiredService<IDocumentProcessingService>();

                    // Process document with cancellation support
                    await processingService.ProcessDocumentAsync(documentId);

                    _logger.LogInformation("Background processing completed successfully for document {DocumentId}", documentId);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Background processing cancelled for document {DocumentId}", documentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background processing failed for document {DocumentId}", documentId);
                }
            }, cancellationToken);

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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Verify document ownership via Test relationship
            var document = await context.StudyDocuments
                .Include(d => d.Test)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null || document.Test == null || document.Test.UserId != userId)
            {
                return NotFound();
            }

            var fileUploadService = scope.ServiceProvider.GetRequiredService<IFileUploadService>();
            var result = await fileUploadService.UpdateExtractedTextAsync(documentId, request.Text, userId);
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
    [MaxLength(1_000_000, ErrorMessage = "Extracted text cannot exceed 1,000,000 characters")]
    public string Text { get; set; } = string.Empty;
}
