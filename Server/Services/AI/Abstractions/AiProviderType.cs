namespace StudieAssistenten.Server.Services.AI.Abstractions;

/// <summary>
/// Supported AI provider types
/// </summary>
public enum AiProviderType
{
    /// <summary>
    /// Anthropic Claude (default, paid)
    /// </summary>
    Anthropic,

    /// <summary>
    /// Google Gemini (free tier available)
    /// </summary>
    Gemini
}
