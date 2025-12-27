using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace StudieAssistenten.Server.Services.AI;

/// <summary>
/// Wraps the Anthropic SDK client and handles API calls with token recording.
/// </summary>
public class AnthropicApiClient : IAnthropicApiClient
{
    private readonly AnthropicClient _client;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnthropicApiClient> _logger;
    private readonly string _model;
    private readonly int _maxTokens;

    public AnthropicApiClient(
        IConfiguration configuration,
        IRateLimitingService rateLimitingService,
        ILogger<AnthropicApiClient> logger)
    {
        _configuration = configuration;
        _rateLimitingService = rateLimitingService;
        _logger = logger;

        var apiKey = configuration["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic API key not configured");
        _model = configuration["Anthropic:Model"] ?? "claude-3-5-sonnet-20241022";
        _maxTokens = int.Parse(configuration["Anthropic:MaxTokens"] ?? "4000");

        if (apiKey == "your-api-key-here" || string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Please set your Anthropic API key in appsettings.Development.json");
        }

        _client = new AnthropicClient(new APIAuthentication(apiKey));
    }

    public async Task<MessageResponse> SendMessageAsync(
        string systemPrompt,
        string userPrompt,
        decimal temperature = 0.7m,
        int? maxTokens = null)
    {
        var messages = new List<Message>
        {
            new Message(RoleType.User, userPrompt)
        };

        // Enable prompt caching for the system prompt to save 90% on repeated content
        var systemMessage = new SystemMessage(systemPrompt)
        {
            CacheControl = new CacheControl { Type = CacheControlType.ephemeral }
        };

        var parameters = new MessageParameters
        {
            Messages = messages,
            MaxTokens = maxTokens ?? _maxTokens,
            Model = _model,
            Stream = false,
            Temperature = temperature,
            System = new List<SystemMessage> { systemMessage }
        };

        _logger.LogInformation("Calling Anthropic API with model: {Model}, Temperature: {Temperature}, MaxTokens: {MaxTokens}",
            parameters.Model, temperature, maxTokens ?? _maxTokens);

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

        return response;
    }
}
