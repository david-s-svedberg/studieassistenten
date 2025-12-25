using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Docnet.Core;
using Docnet.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
    private readonly StudieAssistenten.Server.Infrastructure.Storage.IFileStorage _fileStorage;
    private readonly ILogger<DocumentProcessingService> _logger;

    public DocumentProcessingService(
        ApplicationDbContext context,
        IOcrService ocrService,
        StudieAssistenten.Server.Infrastructure.Storage.IFileStorage fileStorage,
        ILogger<DocumentProcessingService> logger)
    {
        _context = context;
        _ocrService = ocrService;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task ProcessDocumentAsync(int documentId)
    {
        _logger.LogInformation("=== ProcessDocumentAsync started for document {DocumentId} ===", documentId);

        var document = await _context.StudyDocuments.FindAsync(documentId);
        if (document == null)
        {
            _logger.LogWarning("Document {DocumentId} not found", documentId);
            return;
        }

        _logger.LogInformation(
            "Document found: Id={DocumentId}, FileName={FileName}, Status={Status}, FilePath={FilePath}",
            documentId, document.FileName, document.Status, document.OriginalFilePath);

        if (string.IsNullOrEmpty(document.OriginalFilePath) || !await _fileStorage.ExistsAsync(document.OriginalFilePath))
        {
            _logger.LogError("File not found for document {DocumentId}: {FilePath}", documentId, document.OriginalFilePath);
            document.Status = DocumentStatus.OcrFailed;
            await _context.SaveChangesAsync();
            return;
        }

        _logger.LogInformation("File exists at: {FilePath}", document.OriginalFilePath);

        try
        {
            _logger.LogInformation("Processing document {DocumentId}: {FileName}", documentId, document.FileName);

            // Update status to OcrInProgress
            document.Status = DocumentStatus.OcrInProgress;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Document status updated to OcrInProgress");

            string extractedText = string.Empty;

            // Determine processing based on file type
            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
            _logger.LogInformation("File extension detected: {Extension}", extension);

            switch (extension)
            {
                case ".pdf":
                    _logger.LogInformation("Processing as PDF");
                    extractedText = await ProcessPdfAsync(document.OriginalFilePath);
                    break;

                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                case ".tiff":
                case ".tif":
                    _logger.LogInformation("Processing as image with OCR");
                    extractedText = await ProcessImageAsync(document.OriginalFilePath);
                    break;

                case ".txt":
                    _logger.LogInformation("Processing as text file");
                    using (var stream = await _fileStorage.ReadAsync(document.OriginalFilePath))
                    using (var reader = new StreamReader(stream))
                    {
                        extractedText = await reader.ReadToEndAsync();
                    }
                    break;

                default:
                    _logger.LogWarning("Unsupported file type for document {DocumentId}: {Extension}", documentId, extension);
                    document.Status = DocumentStatus.OcrFailed;
                    await _context.SaveChangesAsync();
                    return;
            }

            _logger.LogInformation("Extraction completed. Text length: {Length}", extractedText?.Length ?? 0);

            document.ExtractedText = extractedText;
            document.Status = string.IsNullOrWhiteSpace(extractedText)
                ? DocumentStatus.OcrFailed
                : DocumentStatus.OcrCompleted;

            _logger.LogInformation("Setting document status to: {Status}", document.Status);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "=== Successfully processed document {DocumentId}. Extracted {Length} characters, Status: {Status} ===",
                documentId, extractedText?.Length ?? 0, document.Status);
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

            // If PDF has no text (scanned PDF), convert pages to images and use OCR
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogInformation("PDF appears to be scanned or has no text content. Converting to images for OCR...");
                extractedText = await ProcessScannedPdfAsync(filePath);
            }

            return extractedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF: {FilePath}", filePath);
            throw;
        }
    }

    private async Task<string> ProcessScannedPdfAsync(string filePath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"pdf_ocr_{Guid.NewGuid()}");
        
        try
        {
            Directory.CreateDirectory(tempDir);
            _logger.LogInformation("Converting PDF to images for OCR: {FilePath}", filePath);

            using var docReader = DocLib.Instance.GetDocReader(filePath, new PageDimensions(2000, 2000));
            var textBuilder = new System.Text.StringBuilder();

            for (int i = 0; i < docReader.GetPageCount(); i++)
            {
                _logger.LogInformation("Processing page {PageNum} of {TotalPages}", i + 1, docReader.GetPageCount());

                using var pageReader = docReader.GetPageReader(i);
                var rawBytes = pageReader.GetImage();

                var width = pageReader.GetPageWidth();
                var height = pageReader.GetPageHeight();

                // Save as temporary PNG file
                var tempImagePath = Path.Combine(tempDir, $"page_{i}.png");
                
                // Convert raw bytes to PNG using ImageSharp
                using (var image = Image.LoadPixelData<Bgra32>(rawBytes, width, height))
                {
                    await image.SaveAsPngAsync(tempImagePath);
                }

                // Run OCR on the image
                var pageText = await _ocrService.ExtractTextFromImageAsync(tempImagePath, "swe");
                
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    textBuilder.AppendLine($"--- Page {i + 1} ---");
                    textBuilder.AppendLine(pageText);
                    textBuilder.AppendLine();
                }

                // Clean up temp image immediately
                File.Delete(tempImagePath);
            }

            var result = textBuilder.ToString().Trim();
            _logger.LogInformation("Scanned PDF OCR completed. Extracted {Length} characters from {PageCount} pages",
                result.Length, docReader.GetPageCount());

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing scanned PDF: {FilePath}", filePath);
            throw;
        }
        finally
        {
            // Clean up temp directory
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary directory: {TempDir}", tempDir);
                }
            }
        }
    }

    private async Task<string> ProcessImageAsync(string filePath)
    {
        var tempImagePath = Path.Combine(Path.GetTempPath(), $"preprocessed_{Guid.NewGuid()}.png");

        try
        {
            _logger.LogInformation("Performing OCR on image: {FilePath}", filePath);

            // Preprocess image for better OCR
            using (var stream = await _fileStorage.ReadAsync(filePath))
            using (var image = await SixLabors.ImageSharp.Image.LoadAsync(stream))
            {
                // Convert to grayscale for better OCR
                image.Mutate(x => x.Grayscale());

                // Increase contrast
                image.Mutate(x => x.Contrast(1.5f));

                // Resize if too large (OCR works better on reasonable sizes)
                if (image.Width > 3000 || image.Height > 3000)
                {
                    var scale = Math.Min(3000.0 / image.Width, 3000.0 / image.Height);
                    var newWidth = (int)(image.Width * scale);
                    var newHeight = (int)(image.Height * scale);
                    image.Mutate(x => x.Resize(newWidth, newHeight));
                }

                // Save preprocessed image
                await image.SaveAsPngAsync(tempImagePath);
            }

            // Use OCR service to extract text from preprocessed image
            var text = await _ocrService.ExtractTextFromImageAsync(tempImagePath, "swe");

            _logger.LogInformation("OCR extracted {Length} characters from image", text.Length);

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing OCR on image: {FilePath}", filePath);
            throw;
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempImagePath))
            {
                try
                {
                    File.Delete(tempImagePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp file: {TempPath}", tempImagePath);
                }
            }
        }
    }
}
