using AutoMapper;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Infrastructure.Storage;
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
    Task<DocumentDetailDto?> GetDocumentDetailAsync(int documentId, string userId);
    Task<List<DocumentDto>> GetAllDocumentsAsync(string userId);
    Task<List<DocumentSummaryDto>> GetAllDocumentsSummaryAsync(string userId);
    Task<bool> DeleteDocumentAsync(int documentId, string userId);
    Task<bool> UpdateExtractedTextAsync(int documentId, string extractedText, string userId);
}

public class FileUploadService : IFileUploadService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorage _fileStorage;
    private readonly IMapper _mapper;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(
        ApplicationDbContext context,
        IFileStorage fileStorage,
        IMapper mapper,
        ILogger<FileUploadService> logger)
    {
        _context = context;
        _fileStorage = fileStorage;
        _mapper = mapper;
        _logger = logger;
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

            // Save file using IFileStorage abstraction (includes path traversal protection)
            var filePath = await _fileStorage.SaveAsync(fileStream, uniqueFileName);

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

            // Note: Only log non-sensitive metadata, not file contents
            _logger.LogInformation("Document uploaded successfully: ID {DocumentId}, Size: {Size} bytes, Type: {ContentType}",
                document.Id, uploadDto.FileSizeBytes, uploadDto.ContentType);

            return _mapper.Map<DocumentDto>(document);
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

        return _mapper.Map<DocumentDto>(document);
    }

    public async Task<List<DocumentDto>> GetAllDocumentsAsync(string userId)
    {
        // Get all documents belonging to tests owned by the user
        var documents = await _context.StudyDocuments
            .Include(d => d.Test)
            .Where(d => d.Test != null && d.Test.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return _mapper.Map<List<DocumentDto>>(documents);
    }

    public async Task<DocumentDetailDto?> GetDocumentDetailAsync(int documentId, string userId)
    {
        var document = await _context.StudyDocuments
            .Include(d => d.Test)
            .FirstOrDefaultAsync(d => d.Id == documentId && d.Test != null && d.Test.UserId == userId);

        return document != null ? _mapper.Map<DocumentDetailDto>(document) : null;
    }

    public async Task<List<DocumentSummaryDto>> GetAllDocumentsSummaryAsync(string userId)
    {
        var documents = await _context.StudyDocuments
            .Include(d => d.Test)
            .Where(d => d.Test != null && d.Test.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return _mapper.Map<List<DocumentSummaryDto>>(documents);
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
        if (!string.IsNullOrEmpty(document.OriginalFilePath))
        {
            try
            {
                await _fileStorage.DeleteAsync(document.OriginalFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete physical file: {FilePath}", document.OriginalFilePath);
            }
        }

        _context.StudyDocuments.Remove(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Document deleted: {DocumentId}", documentId);
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

        _logger.LogInformation("Extracted text updated for document: {DocumentId}, Length: {Length} characters",
            documentId, extractedText?.Length ?? 0);
        return true;
    }

}
