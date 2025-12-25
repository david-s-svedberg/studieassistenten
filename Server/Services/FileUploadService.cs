using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace StudieAssistenten.Server.Services;

/// <summary>
/// Service for handling file uploads and document management
/// </summary>
public interface IFileUploadService
{
    Task<DocumentDto> UploadDocumentAsync(Stream fileStream, DocumentUploadDto uploadDto, string userId);
    Task<DocumentDto?> GetDocumentAsync(int documentId, string userId);
    Task<List<DocumentDto>> GetAllDocumentsAsync(string userId);
    Task<bool> DeleteDocumentAsync(int documentId, string userId);
    Task<bool> UpdateExtractedTextAsync(int documentId, string extractedText, string userId);
}

public class FileUploadService : IFileUploadService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FileUploadService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _uploadPath;

    public FileUploadService(
        ApplicationDbContext context,
        ILogger<FileUploadService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        
        // Set up upload directory
        _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<DocumentDto> UploadDocumentAsync(Stream fileStream, DocumentUploadDto uploadDto, string userId)
    {
        try
        {
            // Verify test ownership if testId is provided
            if (uploadDto.TestId.HasValue)
            {
                var test = await _context.Tests
                    .FirstOrDefaultAsync(t => t.Id == uploadDto.TestId.Value && t.UserId == userId);

                if (test == null)
                {
                    throw new UnauthorizedAccessException($"Test {uploadDto.TestId.Value} not found or access denied");
                }
            }

            // Sanitize filename to prevent path traversal attacks
            // Only extract the filename part (remove any path components)
            var safeFileName = Path.GetFileName(uploadDto.FileName);

            // Get extension from sanitized filename
            var fileExtension = Path.GetExtension(safeFileName);

            // Validate extension is not empty and doesn't contain dangerous characters
            if (string.IsNullOrWhiteSpace(fileExtension) || fileExtension.Contains("..") || fileExtension.Length > 10)
            {
                throw new InvalidOperationException("Invalid file extension");
            }

            // Generate unique file name with sanitized extension
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension.ToLowerInvariant()}";
            var filePath = Path.Combine(_uploadPath, uniqueFileName);

            // Final safety check: ensure the resolved path is still within the upload directory
            var fullPath = Path.GetFullPath(filePath);
            var uploadPathFull = Path.GetFullPath(_uploadPath);
            if (!fullPath.StartsWith(uploadPathFull, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected: {FileName}", uploadDto.FileName);
                throw new InvalidOperationException("Invalid file path");
            }

            // Save file to disk
            using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStreamOutput);
            }

            // Create database entry
            var document = new StudyDocument
            {
                FileName = uploadDto.FileName,
                OriginalFilePath = filePath,
                FileSizeBytes = uploadDto.FileSizeBytes,
                ContentType = uploadDto.ContentType,
                TestId = uploadDto.TestId,
                UploadedAt = DateTime.UtcNow,
                Status = DocumentStatus.Uploaded
            };

            _context.StudyDocuments.Add(document);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Document uploaded successfully: {FileName} (ID: {DocumentId}) for User: {UserId}",
                uploadDto.FileName, document.Id, userId);

            return MapToDto(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document: {FileName}", uploadDto.FileName);
            throw;
        }
    }

    public async Task<DocumentDto?> GetDocumentAsync(int documentId, string userId)
    {
        // Get document and verify ownership via Test relationship
        var document = await _context.StudyDocuments
            .Include(d => d.Test)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null || document.Test == null || document.Test.UserId != userId)
        {
            return null;
        }

        return MapToDto(document);
    }

    public async Task<List<DocumentDto>> GetAllDocumentsAsync(string userId)
    {
        // Get all documents belonging to tests owned by the user
        var documents = await _context.StudyDocuments
            .Include(d => d.Test)
            .Where(d => d.Test != null && d.Test.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return documents.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, string userId)
    {
        // Get document and verify ownership via Test relationship
        var document = await _context.StudyDocuments
            .Include(d => d.Test)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null || document.Test == null || document.Test.UserId != userId)
            return false;

        // Delete physical file if exists
        if (!string.IsNullOrEmpty(document.OriginalFilePath) && File.Exists(document.OriginalFilePath))
        {
            try
            {
                File.Delete(document.OriginalFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete physical file: {FilePath}", document.OriginalFilePath);
            }
        }

        _context.StudyDocuments.Remove(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document deleted: {DocumentId} for User: {UserId}", documentId, userId);
        return true;
    }

    public async Task<bool> UpdateExtractedTextAsync(int documentId, string extractedText, string userId)
    {
        // Get document and verify ownership via Test relationship
        var document = await _context.StudyDocuments
            .Include(d => d.Test)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null || document.Test == null || document.Test.UserId != userId)
            return false;

        document.ExtractedText = extractedText;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Extracted text updated for document: {DocumentId} for User: {UserId}", documentId, userId);
        return true;
    }

    private static DocumentDto MapToDto(StudyDocument document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            FileName = document.FileName,
            StoredFileName = !string.IsNullOrEmpty(document.OriginalFilePath)
                ? Path.GetFileName(document.OriginalFilePath)
                : string.Empty,
            ContentType = document.ContentType,
            FileSizeBytes = document.FileSizeBytes,
            UploadedAt = document.UploadedAt,
            Status = document.Status,
            ExtractedText = document.ExtractedText,
            TestId = document.TestId
        };
    }
}
