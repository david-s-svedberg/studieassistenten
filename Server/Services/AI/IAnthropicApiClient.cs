using Anthropic.SDK.Messaging;

namespace StudieAssistenten.Server.Services.AI;

/// <summary>
/// Abstraction over the Anthropic SDK for AI text generation.
/// </summary>
public interface IAnthropicApiClient
{
    /// <summary>
    /// Sends a message to Claude and returns the response.
    /// Automatically records token usage for rate limiting.
    /// </summary>
    Task<MessageResponse> SendMessageAsync(
        string systemPrompt,
        string userPrompt,
        decimal temperature = 0.7m,
        int? maxTokens = null);
}
