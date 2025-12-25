using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudieAssistenten.Server.Services;
using StudieAssistenten.Shared.DTOs;
using System.Security.Claims;

namespace StudieAssistenten.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : BaseApiController
{
    private readonly IFileUploadService _fileUploadService;
    private readonly IFileValidationService _fileValidationService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IFileUploadService fileUploadService,
        IFileValidationService fileValidationService,
        ILogger<DocumentsController> logger)
    {
        _fileUploadService = fileUploadService;
        _fileValidationService = fileValidationService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a new document
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<DocumentDto>> Upload([FromForm] IFormFile file, [FromForm] int? testId)
    {
        try
        {
            var userId = GetCurrentUserId();

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Validate file size (max 50MB)
            const long maxFileSize = 50 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return BadRequest("File size exceeds maximum allowed size of 50MB");
            }

            // Validate file type by Content-Type header
            var allowedContentTypes = new[]
            {
                "application/pdf",
                "image/jpeg",
                "image/jpg",
                "image/png",
                "text/plain",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            };

            if (!allowedContentTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest($"File type '{file.ContentType}' is not supported. Allowed types: PDF, JPEG, PNG, TXT, DOCX");
            }

            // Validate actual file content (magic bytes) to prevent file type spoofing
            using var validationStream = file.OpenReadStream();
            var (isValid, detectedType) = await _fileValidationService.ValidateFileContentAsync(validationStream, file.ContentType);

            if (!isValid)
            {
                _logger.LogWarning(
                    "File content validation failed for {FileName}. Claimed: {ClaimedType}, Detected: {DetectedType}",
                    file.FileName,
                    file.ContentType,
                    detectedType ?? "unknown");

                return BadRequest(
                    $"File content does not match the claimed type '{file.ContentType}'. " +
                    $"This may indicate a malicious file or corrupted upload.");
            }

            var uploadDto = new DocumentUploadDto
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                TestId = testId
            };

            using var stream = file.OpenReadStream();
            var result = await _fileUploadService.UploadDocumentAsync(stream, uploadDto, userId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, "An error occurred while uploading the file");
        }
    }

    /// <summary>
    /// Get all documents (lightweight list view without extracted text)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DocumentSummaryDto>>> GetAll()
    {
        try
        {
            var userId = GetCurrentUserId();

            var documents = await _fileUploadService.GetAllDocumentsSummaryAsync(userId);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents");
            return StatusCode(500, "An error occurred while retrieving documents");
        }
    }

    /// <summary>
    /// Get a specific document (detailed view with extracted text)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentDetailDto>> GetById(int id)
    {
        try
        {
            var userId = GetCurrentUserId();

            var document = await _fileUploadService.GetDocumentDetailAsync(id, userId);
            if (document == null)
            {
                return NotFound();
            }

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId}", id);
            return StatusCode(500, "An error occurred while retrieving the document");
        }
    }

    /// <summary>
    /// Delete a document
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var userId = GetCurrentUserId();

            var result = await _fileUploadService.DeleteDocumentAsync(id, userId);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", id);
            return StatusCode(500, "An error occurred while deleting the document");
        }
    }

    /// <summary>
    /// Download a document file (with authorization)
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<ActionResult> Download(int id)
    {
        try
        {
            var userId = GetCurrentUserId();

            var document = await _fileUploadService.GetDocumentAsync(id, userId);
            if (document == null)
            {
                return NotFound();
            }

            // Get the physical file path
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", document.StoredFileName);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("File not found on disk: {FilePath}", filePath);
                return NotFound("File not found on server");
            }

            // Read the file
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // Return file with appropriate content type
            return File(fileBytes, document.ContentType ?? "application/octet-stream", document.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {DocumentId}", id);
            return StatusCode(500, "An error occurred while downloading the file");
        }
    }
}
