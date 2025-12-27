using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using StudieAssistenten.Server.Services.AI.Abstractions;

namespace StudieAssistenten.Server.Services.AI.Providers;

/// <summary>
/// Anthropic Claude AI provider implementation
/// </summary>
public class AnthropicAiProvider : IAiProvider
{
    private readonly AnthropicClient _client;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnthropicAiProvider> _logger;
    private readonly string _model;
    private readonly int _defaultMaxTokens;

    public AiProviderType ProviderType => AiProviderType.Anthropic;

    public AnthropicAiProvider(
        IConfiguration configuration,
        IRateLimitingService rateLimitingService,
        ILogger<AnthropicAiProvider> logger)
    {
        _configuration = configuration;
        _rateLimitingService = rateLimitingService;
        _logger = logger;

        var apiKey = configuration["Anthropic:ApiKey"];
        _model = configuration["Anthropic:Model"] ?? "claude-3-5-sonnet-20241022";
        _defaultMaxTokens = int.Parse(configuration["Anthropic:MaxTokens"] ?? "4000");

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "your-api-key-here")
        {
            _logger.LogWarning("Anthropic API key not configured. Provider will not be available.");
            _client = null!;
        }
        else
        {
            _client = new AnthropicClient(new APIAuthentication(apiKey));
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
            throw new InvalidOperationException("Anthropic provider is not configured. Please set Anthropic:ApiKey in configuration.");
        }

        var messages = new List<Message>
        {
            new Message(RoleType.User, request.UserPrompt)
        };

        // Enable prompt caching if requested and system prompt is provided
        var systemMessages = new List<SystemMessage>();
        if (!string.IsNullOrEmpty(request.SystemPrompt))
        {
            var systemMessage = new SystemMessage(request.SystemPrompt);

            // Add cache control if caching is enabled
            if (request.EnableCaching)
            {
                systemMessage.CacheControl = new CacheControl { Type = CacheControlType.ephemeral };
            }

            systemMessages.Add(systemMessage);
        }

        var parameters = new MessageParameters
        {
            Messages = messages,
            MaxTokens = request.MaxTokens ?? _defaultMaxTokens,
            Model = _model,
            Stream = false,
            Temperature = request.Temperature,
            System = systemMessages
        };

        _logger.LogInformation("Calling Anthropic API with model: {Model}, Temperature: {Temperature}, MaxTokens: {MaxTokens}, Caching: {Caching}",
            parameters.Model, request.Temperature, request.MaxTokens ?? _defaultMaxTokens, request.EnableCaching);

        MessageResponse response;
        try
        {
            response = await _client.Messages.GetClaudeMessageAsync(parameters);

            // Log cache performance metrics
            var cacheReadTokens = response.Usage.CacheReadInputTokens;
            var cacheCreationTokens = response.Usage.CacheCreationInputTokens;
            var regularInputTokens = response.Usage.InputTokens;

            if (cacheReadTokens > 0)
            {
                _logger.LogInformation("API call successful with CACHE HIT! Response ID: {Id}, " +
                    "CacheRead: {CacheRead}, Regular: {Regular}, Output: {Output} tokens. " +
                    "Cost savings: ~{Savings}%",
                    response.Id, cacheReadTokens, regularInputTokens, response.Usage.OutputTokens,
                    (int)(cacheReadTokens * 100.0 / (cacheReadTokens + regularInputTokens)));
            }
            else if (cacheCreationTokens > 0)
            {
                _logger.LogInformation("API call successful, created cache. Response ID: {Id}, " +
                    "CacheCreation: {CacheCreation}, Regular: {Regular}, Output: {Output} tokens",
                    response.Id, cacheCreationTokens, regularInputTokens, response.Usage.OutputTokens);
            }
            else
            {
                _logger.LogInformation("API call successful (no cache), response ID: {Id}, " +
                    "InputTokens: {InputTokens}, OutputTokens: {OutputTokens}",
                    response.Id, regularInputTokens, response.Usage.OutputTokens);
            }

            // Record token usage for rate limiting
            if (_configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true))
            {
                await _rateLimitingService.RecordUsageAsync(
                    response.Usage.InputTokens,
                    response.Usage.OutputTokens);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic API call failed. Model: {Model}, Error: {ErrorMessage}", _model, ex.Message);
            throw;
        }

        // Map to common response model
        return new AiResponse
        {
            Id = response.Id,
            Content = (response.Content.FirstOrDefault() as TextContent)?.Text ?? string.Empty,
            Model = response.Model,
            Provider = AiProviderType.Anthropic,
            StopReason = response.StopReason,
            Usage = new AiUsage
            {
                InputTokens = response.Usage.InputTokens,
                OutputTokens = response.Usage.OutputTokens,
                CacheReadTokens = response.Usage.CacheReadInputTokens,
                CacheCreationTokens = response.Usage.CacheCreationInputTokens
            }
        };
    }
}
