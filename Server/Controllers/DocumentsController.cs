using Microsoft.AspNetCore.Mvc;
using StudieAssistenten.Server.Services;
using StudieAssistenten.Shared.DTOs;

namespace StudieAssistenten.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IFileUploadService fileUploadService,
        ILogger<DocumentsController> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a new document
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<DocumentDto>> Upload([FromForm] IFormFile file, [FromForm] string? teacherInstructions)
    {
        try
        {
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

            // Validate file type
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

            var uploadDto = new DocumentUploadDto
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                TeacherInstructions = teacherInstructions
            };

            using var stream = file.OpenReadStream();
            var result = await _fileUploadService.UploadDocumentAsync(stream, uploadDto);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, "An error occurred while uploading the file");
        }
    }

    /// <summary>
    /// Get all documents
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DocumentDto>>> GetAll()
    {
        try
        {
            var documents = await _fileUploadService.GetAllDocumentsAsync();
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents");
            return StatusCode(500, "An error occurred while retrieving documents");
        }
    }

    /// <summary>
    /// Get a specific document
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DocumentDto>> GetById(int id)
    {
        try
        {
            var document = await _fileUploadService.GetDocumentAsync(id);
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
            var result = await _fileUploadService.DeleteDocumentAsync(id);
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
}
