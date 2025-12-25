using StudieAssistenten.Shared.Enums;
using Azure;
using Azure.AI.Vision.ImageAnalysis;

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

    public Task<string> ExtractTextFromImageAsync(string imagePath, string language = "swe")
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
                return Task.FromResult(string.Empty);
            }

            return Task.FromResult(text.Trim());
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

/// <summary>
/// Azure Computer Vision OCR service implementation
/// </summary>
public class AzureComputerVisionOcrService : IOcrService
{
    private readonly ILogger<AzureComputerVisionOcrService> _logger;
    private readonly ImageAnalysisClient? _client;
    private readonly string? _endpoint;
    private readonly string? _apiKey;

    public AzureComputerVisionOcrService(
        ILogger<AzureComputerVisionOcrService> logger,
        IConfiguration configuration)
    {
        _logger = logger;

        // Read from config, but fall back to environment variables if empty or placeholder
        var configEndpoint = configuration["Azure:ComputerVision:Endpoint"];
        var configApiKey = configuration["Azure:ComputerVision:ApiKey"];

        _endpoint = !string.IsNullOrEmpty(configEndpoint) && !configEndpoint.Contains("YOUR_")
            ? configEndpoint
            : Environment.GetEnvironmentVariable("AZURE_VISION_ENDPOINT");

        _apiKey = !string.IsNullOrEmpty(configApiKey) && !configApiKey.Contains("YOUR_")
            ? configApiKey
            : Environment.GetEnvironmentVariable("AZURE_VISION_KEY");

        // Log where credentials were loaded from
        var endpointSource = !string.IsNullOrEmpty(configEndpoint) && !configEndpoint.Contains("YOUR_")
            ? "appsettings.json"
            : "environment variable";
        var apiKeySource = !string.IsNullOrEmpty(configApiKey) && !configApiKey.Contains("YOUR_")
            ? "appsettings.json"
            : "environment variable";

        _logger.LogInformation(
            "Azure Computer Vision credential sources - Endpoint: {EndpointSource}, ApiKey: {ApiKeySource}",
            endpointSource, apiKeySource);

        if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_apiKey) ||
            _endpoint.Contains("YOUR_") || _apiKey.Contains("YOUR_"))
        {
            _logger.LogWarning(
                "Azure Computer Vision credentials not configured properly. " +
                "Endpoint: '{Endpoint}', ApiKey configured: {HasKey}. " +
                "Set Azure:ComputerVision:Endpoint and Azure:ComputerVision:ApiKey in appsettings.Development.json " +
                "or AZURE_VISION_ENDPOINT and AZURE_VISION_KEY environment variables.",
                _endpoint ?? "(null)", !string.IsNullOrEmpty(_apiKey));
        }
        else if (!Uri.TryCreate(_endpoint, UriKind.Absolute, out _))
        {
            _logger.LogError(
                "Azure Computer Vision endpoint is not a valid URI: {Endpoint}. " +
                "Expected format: https://your-resource.cognitiveservices.azure.com/",
                _endpoint);
        }
        else
        {
            try
            {
                _client = new ImageAnalysisClient(
                    new Uri(_endpoint),
                    new AzureKeyCredential(_apiKey));
                _logger.LogInformation(
                    "Azure Computer Vision initialized successfully with endpoint: {Endpoint}",
                    _endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Azure Computer Vision client");
            }
        }
    }

    public async Task<string> ExtractTextFromImageAsync(string imagePath, string language = "swe")
    {
        _logger.LogInformation("ExtractTextFromImageAsync called for: {FilePath}", imagePath);

        if (_client == null)
        {
            var errorMsg = "Azure Computer Vision is not configured. Please set the endpoint and API key.";
            _logger.LogError(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        if (!File.Exists(imagePath))
        {
            _logger.LogError("Image file not found: {FilePath}", imagePath);
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        try
        {
            _logger.LogInformation("Opening image file: {FilePath}", imagePath);

            using var imageStream = File.OpenRead(imagePath);
            var imageData = BinaryData.FromStream(imageStream);

            _logger.LogInformation(
                "Calling Azure Computer Vision API with {Size} bytes, language: sv",
                imageData.ToArray().Length);

            // Analyze image with Read feature
            var result = await _client.AnalyzeAsync(
                imageData,
                VisualFeatures.Read,
                new ImageAnalysisOptions { Language = "sv" }); // Swedish language

            _logger.LogInformation("Azure API call completed. Result: {HasRead}", result.Value.Read != null);

            if (result.Value.Read == null || result.Value.Read.Blocks.Count == 0)
            {
                _logger.LogWarning("Azure OCR returned no text blocks for file: {FilePath}", imagePath);
                return string.Empty;
            }

            _logger.LogInformation(
                "Azure OCR found {BlockCount} blocks",
                result.Value.Read.Blocks.Count);

            // Extract text from all blocks and lines
            var textBuilder = new System.Text.StringBuilder();
            foreach (var block in result.Value.Read.Blocks)
            {
                _logger.LogInformation("Processing block with {LineCount} lines", block.Lines.Count);
                foreach (var line in block.Lines)
                {
                    _logger.LogDebug("Line text: {Text}", line.Text);
                    textBuilder.AppendLine(line.Text);
                }
            }

            var extractedText = textBuilder.ToString().Trim();
            _logger.LogInformation(
                "Azure OCR completed successfully. Extracted {Length} characters. First 100 chars: {Preview}",
                extractedText.Length,
                extractedText.Length > 100 ? extractedText.Substring(0, 100) : extractedText);

            return extractedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing Azure OCR on file: {FilePath}. Exception type: {ExceptionType}, Message: {Message}",
                imagePath, ex.GetType().Name, ex.Message);
            throw;
        }
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(_client != null &&
                               !string.IsNullOrEmpty(_endpoint) &&
                               !string.IsNullOrEmpty(_apiKey));
    }
}
