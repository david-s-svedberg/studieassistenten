namespace StudieAssistenten.Server.Services.AI.Abstractions;

/// <summary>
/// Common interface for AI providers (Anthropic, Gemini, etc.)
/// </summary>
public interface IAiProvider
{
    /// <summary>
    /// The type of provider
    /// </summary>
    AiProviderType ProviderType { get; }

    /// <summary>
    /// Send a message to the AI and get a response
    /// </summary>
    /// <param name="request">The request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The AI response</returns>
    Task<AiResponse> SendMessageAsync(AiRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the provider is properly configured
    /// </summary>
    /// <returns>True if configured and ready to use</returns>
    bool IsConfigured();
}
