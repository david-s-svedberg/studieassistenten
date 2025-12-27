namespace StudieAssistenten.Server.Services.AI.Abstractions;

/// <summary>
/// Common response model for AI API calls
/// </summary>
public class AiResponse
{
    /// <summary>
    /// Unique identifier for this response
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The generated text content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Token usage information
    /// </summary>
    public AiUsage Usage { get; set; } = new();

    /// <summary>
    /// The model that generated this response
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// The provider that generated this response
    /// </summary>
    public AiProviderType Provider { get; set; }

    /// <summary>
    /// Reason the generation stopped (e.g., "end_turn", "max_tokens")
    /// </summary>
    public string? StopReason { get; set; }
}
