namespace StudieAssistenten.Server.Services.AI.Abstractions;

/// <summary>
/// Token usage information from AI API call
/// </summary>
public class AiUsage
{
    /// <summary>
    /// Number of input tokens used
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Number of output tokens generated
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Number of cached input tokens read (for providers that support caching)
    /// </summary>
    public int CacheReadTokens { get; set; }

    /// <summary>
    /// Number of tokens written to cache (for providers that support caching)
    /// </summary>
    public int CacheCreationTokens { get; set; }

    /// <summary>
    /// Total tokens used (input + output)
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens;

    /// <summary>
    /// Whether this call used cached content
    /// </summary>
    public bool UsedCache => CacheReadTokens > 0;

    /// <summary>
    /// Whether this call created a cache
    /// </summary>
    public bool CreatedCache => CacheCreationTokens > 0;
}
