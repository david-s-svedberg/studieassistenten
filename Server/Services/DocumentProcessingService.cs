using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace StudieAssistenten.Server.Services;

/// <summary>
/// Service for processing uploaded documents and extracting text
/// </summary>
public interface IDocumentProcessingService
{
    Task ProcessDocumentAsync(int documentId);
}

public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly ApplicationDbContext _context;
    private readonly IOcrService _ocrService;
    private readonly ILogger<DocumentProcessingService> _logger;

    public DocumentProcessingService(
        ApplicationDbContext context,
        IOcrService ocrService,
        ILogger<DocumentProcessingService> logger)
    {
        _context = context;
        _ocrService = ocrService;
        _logger = logger;
    }

    public async Task ProcessDocumentAsync(int documentId)
    {
        var document = await _context.StudyDocuments.FindAsync(documentId);
        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found", documentId);
            return;
        }

        if (string.IsNullOrEmpty(document.OriginalFilePath) || !File.Exists(document.OriginalFilePath))
        {
            _logger.LogError("File not found for document {DocumentId}: {FilePath}", documentId, document.OriginalFilePath);
            document.Status = DocumentStatus.OcrFailed;
            await _context.SaveChangesAsync();
            return;
        }

        try
        {
            _logger.LogInformation("Processing document {DocumentId}: {FileName}", documentId, document.FileName);

            string extractedText = string.Empty;

            // Determine processing based on file type
            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();

            switch (extension)
            {
                case ".pdf":
                    extractedText = await ProcessPdfAsync(document.OriginalFilePath);
                    break;

                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                case ".tiff":
                case ".tif":
                    extractedText = await ProcessImageAsync(document.OriginalFilePath);
                    break;

                case ".txt":
                    extractedText = await File.ReadAllTextAsync(document.OriginalFilePath);
                    break;

                default:
                    _logger.LogWarning("Unsupported file type for document {DocumentId}: {Extension}", documentId, extension);
                    document.Status = DocumentStatus.OcrFailed;
                    await _context.SaveChangesAsync();
                    return;
            }

            document.ExtractedText = extractedText;
            document.Status = string.IsNullOrWhiteSpace(extractedText) 
                ? DocumentStatus.OcrFailed 
                : DocumentStatus.OcrCompleted;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully processed document {DocumentId}. Extracted {Length} characters",
                documentId, extractedText.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {DocumentId}", documentId);
            document.Status = DocumentStatus.OcrFailed;
            await _context.SaveChangesAsync();
        }
    }

    private async Task<string> ProcessPdfAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Extracting text from PDF: {FilePath}", filePath);

            using var document = PdfDocument.Open(filePath);
            var textBuilder = new System.Text.StringBuilder();

            foreach (var page in document.GetPages())
            {
                var text = page.Text;
                textBuilder.AppendLine(text);
            }

            var extractedText = textBuilder.ToString().Trim();

            // If PDF has no text (scanned PDF), we would need to convert to images and use OCR
            // For now, we'll just return what we got
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogWarning("PDF appears to be scanned or has no text content");
                // TODO: Convert PDF pages to images and run OCR
            }

            return extractedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF: {FilePath}", filePath);
            throw;
        }
    }

    private async Task<string> ProcessImageAsync(string filePath)
    {
        try
        {
            _logger.LogInformation("Performing OCR on image: {FilePath}", filePath);

            // Use OCR service to extract text from image
            var text = await _ocrService.ExtractTextFromImageAsync(filePath, "swe");
            
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing OCR on image: {FilePath}", filePath);
            throw;
        }
    }
}
