using Anthropic.SDK.Messaging;
using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services.AI.Generators;

public interface ISummaryGenerator
{
    Task<GeneratedContent> GenerateAsync(int testId, string? teacherInstructions = null);
}

public class SummaryGenerator : BaseContentGenerator, ISummaryGenerator
{
    private readonly IAnthropicApiClient _apiClient;

    public SummaryGenerator(
        IAnthropicApiClient apiClient,
        ApplicationDbContext context,
        IRateLimitingService rateLimitingService,
        IConfiguration configuration,
        ILogger<SummaryGenerator> logger)
        : base(context, rateLimitingService, configuration, logger)
    {
        _apiClient = apiClient;
    }

    public async Task<GeneratedContent> GenerateAsync(int testId, string? teacherInstructions = null)
    {
        await CheckRateLimitAsync();

        // Get test with all documents
        var test = await Context.Tests
            .Include(t => t.Documents)
            .FirstOrDefaultAsync(t => t.Id == testId);

        if (test == null)
        {
            throw new InvalidOperationException($"Test {testId} not found");
        }

        if (!test.Documents.Any())
        {
            throw new InvalidOperationException($"Test {testId} has no documents");
        }

        // Combine text from all documents
        var combinedText = string.Join("\n\n--- Next Document ---\n\n",
            test.Documents
                .Where(d => !string.IsNullOrWhiteSpace(d.ExtractedText))
                .Select(d => $"Document: {d.FileName}\n{d.ExtractedText}"));

        if (string.IsNullOrWhiteSpace(combinedText))
        {
            throw new InvalidOperationException($"No text content available in test {testId} documents");
        }

        Logger.LogInformation("Generating summary for test {TestId} with {DocumentCount} documents",
            testId, test.Documents.Count);

        var systemPrompt = @"You are an educational assistant that creates concise summaries of study materials.
Create a clear, structured summary in Swedish that captures the key concepts and important details.
Use bullet points, headings, and formatting to make the summary easy to scan and understand.
Focus on what students need to know for studying and test preparation.";

        var userPrompt = $@"Create a comprehensive but concise summary of the following study material:

{combinedText}

{(string.IsNullOrWhiteSpace(teacherInstructions) ? "" : $"\nAdditional instructions: {teacherInstructions}")}";

        var response = await _apiClient.SendMessageAsync(systemPrompt, userPrompt, temperature: 0.5m);

        var content = (response.Content.FirstOrDefault() as TextContent)?.Text ?? string.Empty;

        var generatedContent = new GeneratedContent
        {
            TestId = testId,
            StudyDocumentId = null, // Generated from all documents in test
            ProcessingType = ProcessingType.Summary,
            GeneratedAt = DateTime.UtcNow,
            Title = $"Summary - {test.Name}",
            Content = content
        };

        Context.GeneratedContents.Add(generatedContent);
        await Context.SaveChangesAsync();

        Logger.LogInformation("Created summary for test {TestId}", testId);

        return generatedContent;
    }
}
