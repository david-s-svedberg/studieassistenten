using StudieAssistenten.Server.Services.AI.Abstractions;

namespace StudieAssistenten.Server.Services.AI;

/// <summary>
/// Factory for creating and selecting AI providers
/// </summary>
public class AiProviderFactory
{
    private readonly IEnumerable<IAiProvider> _providers;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiProviderFactory> _logger;

    public AiProviderFactory(
        IEnumerable<IAiProvider> providers,
        IConfiguration configuration,
        ILogger<AiProviderFactory> logger)
    {
        _providers = providers;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Get the configured AI provider
    /// </summary>
    /// <returns>The configured provider</returns>
    /// <exception cref="InvalidOperationException">If no provider is configured</exception>
    public IAiProvider GetProvider()
    {
        // Get configured provider type from settings
        var providerTypeSetting = _configuration["AI:Provider"] ?? "Anthropic";

        if (!Enum.TryParse<AiProviderType>(providerTypeSetting, ignoreCase: true, out var providerType))
        {
            _logger.LogWarning("Invalid AI provider type '{Provider}' in configuration. Defaulting to Anthropic.", providerTypeSetting);
            providerType = AiProviderType.Anthropic;
        }

        // Find the provider
        var provider = _providers.FirstOrDefault(p => p.ProviderType == providerType);

        if (provider == null)
        {
            throw new InvalidOperationException($"AI provider '{providerType}' not found. Available providers: {string.Join(", ", _providers.Select(p => p.ProviderType))}");
        }

        if (!provider.IsConfigured())
        {
            // Try to fall back to another configured provider
            var fallbackProvider = _providers.FirstOrDefault(p => p.IsConfigured());

            if (fallbackProvider != null)
            {
                _logger.LogWarning("Configured provider '{Provider}' is not available. Falling back to '{FallbackProvider}'.",
                    providerType, fallbackProvider.ProviderType);
                return fallbackProvider;
            }

            throw new InvalidOperationException($"AI provider '{providerType}' is not configured. Please set the API key in configuration.");
        }

        _logger.LogInformation("Using AI provider: {Provider}", providerType);
        return provider;
    }

    /// <summary>
    /// Get a specific provider by type
    /// </summary>
    /// <param name="providerType">The provider type</param>
    /// <returns>The provider</returns>
    /// <exception cref="InvalidOperationException">If provider not found or not configured</exception>
    public IAiProvider GetProvider(AiProviderType providerType)
    {
        var provider = _providers.FirstOrDefault(p => p.ProviderType == providerType);

        if (provider == null)
        {
            throw new InvalidOperationException($"AI provider '{providerType}' not found.");
        }

        if (!provider.IsConfigured())
        {
            throw new InvalidOperationException($"AI provider '{providerType}' is not configured.");
        }

        return provider;
    }

    /// <summary>
    /// Get all configured providers
    /// </summary>
    /// <returns>List of configured providers</returns>
    public IEnumerable<IAiProvider> GetConfiguredProviders()
    {
        return _providers.Where(p => p.IsConfigured());
    }
}
