namespace StudieAssistenten.Shared.Models;

/// <summary>
/// Tracks daily API token usage for rate limiting
/// </summary>
public class UsageTracking
{
    public int Id { get; set; }

    /// <summary>
    /// Date for this usage record (stored as date only, no time component)
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Total input tokens used on this date
    /// </summary>
    public long InputTokens { get; set; }

    /// <summary>
    /// Total output tokens used on this date
    /// </summary>
    public long OutputTokens { get; set; }

    /// <summary>
    /// Total tokens (input + output)
    /// </summary>
    public long TotalTokens => InputTokens + OutputTokens;

    /// <summary>
    /// Number of API calls made on this date
    /// </summary>
    public int ApiCallCount { get; set; }

    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; set; }
}
