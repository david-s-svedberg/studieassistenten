using StudieAssistenten.Shared.Enums;

namespace StudieAssistenten.Server.Services;

/// <summary>
/// Service for performing OCR (Optical Character Recognition) on images and documents
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Extract text from an image file
    /// </summary>
    Task<string> ExtractTextFromImageAsync(string imagePath, string language = "swe");
    
    /// <summary>
    /// Check if OCR service is ready and language data is available
    /// </summary>
    Task<bool> IsAvailableAsync();
}

/// <summary>
/// Tesseract-based OCR service implementation
/// </summary>
public class TesseractOcrService : IOcrService
{
    private readonly ILogger<TesseractOcrService> _logger;
    private readonly string _tessDataPath;
    
    public TesseractOcrService(ILogger<TesseractOcrService> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        // Set up tessdata directory
        _tessDataPath = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");
        if (!Directory.Exists(_tessDataPath))
        {
            Directory.CreateDirectory(_tessDataPath);
        }
    }

    public async Task<string> ExtractTextFromImageAsync(string imagePath, string language = "swe")
    {
        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        try
        {
            _logger.LogInformation("Starting OCR on file: {FilePath}", imagePath);

            // Check if language data exists
            var langFile = Path.Combine(_tessDataPath, $"{language}.traineddata");
            if (!File.Exists(langFile))
            {
                _logger.LogWarning("Language file {Language} not found. Attempting English fallback.", language);
                language = "eng"; // Fallback to English
                
                langFile = Path.Combine(_tessDataPath, $"{language}.traineddata");
                if (!File.Exists(langFile))
                {
                    throw new InvalidOperationException(
                        "OCR language data not found. Please ensure Tesseract language files are installed in the tessdata directory.");
                }
            }

            // Perform OCR using Tesseract
            using var engine = new Tesseract.TesseractEngine(_tessDataPath, language, Tesseract.EngineMode.Default);
            using var img = Tesseract.Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);
            
            var text = page.GetText();
            var confidence = page.GetMeanConfidence();

            _logger.LogInformation("OCR completed with confidence: {Confidence:P}", confidence);

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("OCR returned empty text for file: {FilePath}", imagePath);
                return string.Empty;
            }

            return text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing OCR on file: {FilePath}", imagePath);
            throw;
        }
    }

    public Task<bool> IsAvailableAsync()
    {
        try
        {
            // Check if at least Swedish or English language data exists
            var sweExists = File.Exists(Path.Combine(_tessDataPath, "swe.traineddata"));
            var engExists = File.Exists(Path.Combine(_tessDataPath, "eng.traineddata"));
            
            return Task.FromResult(sweExists || engExists);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
