namespace StudieAssistenten.Server.Services.AI.Abstractions;

/// <summary>
/// Common request model for AI API calls
/// </summary>
public class AiRequest
{
    /// <summary>
    /// System prompt (instructions for the AI)
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>
    /// User prompt (the specific request/question)
    /// </summary>
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>
    /// Temperature for randomness (0.0 = deterministic, 1.0 = creative)
    /// Default: 0.7
    /// </summary>
    public decimal Temperature { get; set; } = 0.7m;

    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Whether to enable prompt caching (if provider supports it)
    /// Default: true
    /// </summary>
    public bool EnableCaching { get; set; } = true;
}
