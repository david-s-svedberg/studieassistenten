using Mscc.GenerativeAI;
using StudieAssistenten.Server.Services.AI.Abstractions;

namespace StudieAssistenten.Server.Services.AI.Providers;

/// <summary>
/// Google Gemini AI provider implementation
/// </summary>
public class GeminiAiProvider : IAiProvider
{
    private readonly GoogleAI? _client;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAiProvider> _logger;
    private readonly string _modelName;

    public AiProviderType ProviderType => AiProviderType.Gemini;

    public GeminiAiProvider(
        IConfiguration configuration,
        IRateLimitingService rateLimitingService,
        ILogger<GeminiAiProvider> logger)
    {
        _configuration = configuration;
        _rateLimitingService = rateLimitingService;
        _logger = logger;

        var apiKey = configuration["Gemini:ApiKey"];
        var modelConfig = configuration["Gemini:Model"] ?? "gemini-2.5-flash";
        _modelName = modelConfig;

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "your-api-key-here")
        {
            _logger.LogWarning("Gemini API key not configured. Provider will not be available.");
            _client = null;
        }
        else
        {
            _client = new GoogleAI(apiKey);
        }
    }

    public bool IsConfigured()
    {
        return _client != null;
    }

    private string GetModelEnum(string modelName)
    {
        // Map configuration strings to SDK model names
        // The SDK expects specific model names (not the enum in newer versions)
        return modelName.ToLowerInvariant() switch
        {
            "gemini-2.5-flash" or "gemini-25-flash" => "gemini-2.5-flash",
            "gemini-2.5-pro" or "gemini-25-pro" => "gemini-2.5-pro",
            "gemini-2.5-flash-lite" => "gemini-2.5-flash-lite",
            "gemini-2.0-flash" or "gemini-20-flash" => "gemini-2.0-flash",
            "gemini-3-flash-preview" => "gemini-3-flash-preview",
            "gemini-3-pro-preview" => "gemini-3-pro-preview",
            _ => modelName // Use as-is if no mapping found
        };
    }

    public async Task<AiResponse> SendMessageAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            throw new InvalidOperationException("Gemini provider is not configured. Please set Gemini:ApiKey in configuration.");
        }

        var modelName = GetModelEnum(_modelName);
        _logger.LogInformation("Calling Gemini API with model: {Model}, Temperature: {Temperature}, MaxTokens: {MaxTokens}",
            modelName, request.Temperature, request.MaxTokens);

        try
        {
            var model = _client!.GenerativeModel(model: modelName);

            // Build the full prompt (system + user)
            var fullPrompt = string.IsNullOrEmpty(request.SystemPrompt)
                ? request.UserPrompt
                : $"{request.SystemPrompt}\n\n{request.UserPrompt}";

            // Note: Gemini SDK doesn't support inline generation config in GenerateContent
            // For now, use simple generation. Advanced config would require model configuration
            var response = await model.GenerateContent(fullPrompt);

            if (response?.Text == null)
            {
                throw new InvalidOperationException("Gemini API returned empty response");
            }

            // Extract token usage (Gemini provides this in UsageMetadata)
            var inputTokens = response.UsageMetadata?.PromptTokenCount ?? 0;
            var outputTokens = response.UsageMetadata?.CandidatesTokenCount ?? 0;
            var cacheReadTokens = response.UsageMetadata?.CachedContentTokenCount ?? 0;

            _logger.LogInformation("Gemini API call successful. InputTokens: {Input}, OutputTokens: {Output}, CacheRead: {CacheRead}",
                inputTokens, outputTokens, cacheReadTokens);

            // Record token usage for rate limiting
            if (_configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true))
            {
                await _rateLimitingService.RecordUsageAsync(inputTokens, outputTokens);
            }

            // Map to common response model
            return new AiResponse
            {
                Id = Guid.NewGuid().ToString(), // Gemini doesn't provide response IDs
                Content = response.Text,
                Model = modelName,
                Provider = AiProviderType.Gemini,
                StopReason = response.Candidates?.FirstOrDefault()?.FinishReason?.ToString(),
                Usage = new AiUsage
                {
                    InputTokens = inputTokens,
                    OutputTokens = outputTokens,
                    CacheReadTokens = cacheReadTokens,
                    CacheCreationTokens = 0 // Gemini handles caching differently
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API call failed. Model: {Model}, Error: {ErrorMessage}", modelName, ex.Message);
            throw;
        }
    }
}
