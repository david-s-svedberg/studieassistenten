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
    private readonly string _model;

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
        _model = configuration["Gemini:Model"] ?? "gemini-2.0-flash-exp";

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

    public async Task<AiResponse> SendMessageAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            throw new InvalidOperationException("Gemini provider is not configured. Please set Gemini:ApiKey in configuration.");
        }

        _logger.LogInformation("Calling Gemini API with model: {Model}, Temperature: {Temperature}, MaxTokens: {MaxTokens}",
            _model, request.Temperature, request.MaxTokens);

        try
        {
            var model = _client!.GenerativeModel(model: _model);

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
                Model = _model,
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
            _logger.LogError(ex, "Gemini API call failed. Model: {Model}, Error: {ErrorMessage}", _model, ex.Message);
            throw;
        }
    }
}
