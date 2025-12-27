using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Services.AI.Abstractions;

namespace StudieAssistenten.Server.Services.AI;

public interface ITestNamingService
{
    Task<string> SuggestTestNameAsync(int testId);
}

public class TestNamingService : BaseContentGenerator, ITestNamingService
{
    private readonly AiProviderFactory _aiProviderFactory;

    public TestNamingService(
        AiProviderFactory aiProviderFactory,
        ApplicationDbContext context,
        IRateLimitingService rateLimitingService,
        IConfiguration configuration,
        ILogger<TestNamingService> logger)
        : base(context, rateLimitingService, configuration, logger)
    {
        _aiProviderFactory = aiProviderFactory;
    }

    public async Task<string> SuggestTestNameAsync(int testId)
    {
        // Check rate limit - if exceeded, return default name instead of throwing
        if (Configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true))
        {
            var canMakeRequest = await RateLimitingService.CanMakeRequestAsync();
            if (!canMakeRequest)
            {
                Logger.LogWarning("Daily token limit exceeded, returning default test name");
                return $"Test - {DateTime.Now:yyyy-MM-dd}";
            }
        }

        // Get all documents for this test
        var documents = await Context.StudyDocuments
            .Where(d => d.TestId == testId && d.ExtractedText != null)
            .ToListAsync();

        if (!documents.Any())
        {
            Logger.LogWarning("No documents with extracted text found for test {TestId}", testId);
            return $"Test - {DateTime.Now:yyyy-MM-dd}";
        }

        // Combine text from all documents (limit to avoid token limits)
        var combinedText = string.Join("\n\n", documents
            .Select(d => d.ExtractedText?.Substring(0, Math.Min(d.ExtractedText.Length, 1000)))
            .Where(t => !string.IsNullOrWhiteSpace(t)));

        if (string.IsNullOrWhiteSpace(combinedText))
        {
            return $"Test - {DateTime.Now:yyyy-MM-dd}";
        }

        var systemPrompt = @"You are an educational assistant that creates concise, descriptive test names.
Based on the content provided, suggest a short, clear test name in Swedish (max 50 characters).
The name should indicate the subject or topic being covered.
Respond with ONLY the test name, nothing else.
Examples: 'Fotosyntesen och Cellbiologi', 'Svenska Grammatik - Verb', 'Andra Världskriget 1939-1945'";

        var userPrompt = $@"Based on this study material, suggest a concise test name (max 50 characters):

{combinedText}";

        Logger.LogInformation("Suggesting test name for test {TestId}", testId);

        try
        {
            var aiRequest = new AiRequest
            {
                SystemPrompt = systemPrompt,
                UserPrompt = userPrompt,
                Temperature = 0.7m,
                MaxTokens = 100,
                EnableCaching = false // Don't cache for short naming requests
            };

            var provider = _aiProviderFactory.GetProvider();
            var response = await provider.SendMessageAsync(aiRequest);

            var suggestedName = response.Content.Trim();

            // Remove quotes if present
            suggestedName = suggestedName.Trim('"', '\'');

            // Limit length
            if (suggestedName.Length > 50)
            {
                suggestedName = suggestedName.Substring(0, 47) + "...";
            }

            Logger.LogInformation("Suggested test name for test {TestId}: {Name}", testId, suggestedName);

            return string.IsNullOrWhiteSpace(suggestedName)
                ? $"Test - {DateTime.Now:yyyy-MM-dd}"
                : suggestedName;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error suggesting test name for test {TestId}", testId);
            return $"Test - {DateTime.Now:yyyy-MM-dd}";
        }
    }
}
