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
    Task<DocumentDto> UploadDocumentAsync(Stream fileStream, DocumentUploadDto uploadDto);
    Task<DocumentDto?> GetDocumentAsync(int documentId);
    Task<List<DocumentDto>> GetAllDocumentsAsync();
    Task<bool> DeleteDocumentAsync(int documentId);
    Task<bool> UpdateExtractedTextAsync(int documentId, string extractedText);
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

    public async Task<DocumentDto> UploadDocumentAsync(Stream fileStream, DocumentUploadDto uploadDto)
    {
        try
        {
            // Generate unique file name
            var fileExtension = Path.GetExtension(uploadDto.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(_uploadPath, uniqueFileName);

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

            _logger.LogInformation("Document uploaded successfully: {FileName} (ID: {DocumentId})", 
                uploadDto.FileName, document.Id);

            return MapToDto(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document: {FileName}", uploadDto.FileName);
            throw;
        }
    }

    public async Task<DocumentDto?> GetDocumentAsync(int documentId)
    {
        var document = await _context.StudyDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId);

        return document != null ? MapToDto(document) : null;
    }

    public async Task<List<DocumentDto>> GetAllDocumentsAsync()
    {
        var documents = await _context.StudyDocuments
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        return documents.Select(MapToDto).ToList();
    }

    public async Task<bool> DeleteDocumentAsync(int documentId)
    {
        var document = await _context.StudyDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
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

        _logger.LogInformation("Document deleted: {DocumentId}", documentId);
        return true;
    }

    public async Task<bool> UpdateExtractedTextAsync(int documentId, string extractedText)
    {
        var document = await _context.StudyDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            return false;

        document.ExtractedText = extractedText;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Extracted text updated for document: {DocumentId}", documentId);
        return true;
    }

    private static DocumentDto MapToDto(StudyDocument document)
    {
        return new DocumentDto
        {
            Id = document.Id,
            FileName = document.FileName,
            FileSizeBytes = document.FileSizeBytes,
            UploadedAt = document.UploadedAt,
            Status = document.Status,
            ExtractedText = document.ExtractedText,
            TestId = document.TestId
        };
    }
}
