using Anthropic.SDK.Messaging;
using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services.AI.Generators;

public interface IPracticeTestGenerator
{
    Task<GeneratedContent> GenerateAsync(int testId, string? teacherInstructions = null);
}

public class PracticeTestGenerator : BaseContentGenerator, IPracticeTestGenerator
{
    private readonly IAnthropicApiClient _apiClient;

    public PracticeTestGenerator(
        IAnthropicApiClient apiClient,
        ApplicationDbContext context,
        IRateLimitingService rateLimitingService,
        IConfiguration configuration,
        ILogger<PracticeTestGenerator> logger)
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

        Logger.LogInformation("Generating practice test for test {TestId} with {DocumentCount} documents",
            testId, test.Documents.Count);

        var systemPrompt = @"You are an educational assistant that creates practice tests from study materials.
Create a practice test in Swedish with multiple choice questions, short answer questions, or essay questions.
Make the questions challenging but fair, covering the key concepts from the material.
Format your response as markdown with clear numbering and formatting.";

        var userPrompt = $@"Create a practice test with 5-10 questions from the following study material:

{combinedText}

Include a mix of question types (multiple choice, short answer) and provide an answer key at the end.

{(string.IsNullOrWhiteSpace(teacherInstructions) ? "" : $"\nAdditional instructions: {teacherInstructions}")}";

        var response = await _apiClient.SendMessageAsync(systemPrompt, userPrompt, temperature: 0.7m);

        var content = (response.Content.FirstOrDefault() as TextContent)?.Text ?? string.Empty;

        var generatedContent = new GeneratedContent
        {
            TestId = testId,
            StudyDocumentId = null, // Generated from all documents in test
            ProcessingType = ProcessingType.PracticeTest,
            GeneratedAt = DateTime.UtcNow,
            Title = $"Practice Test - {test.Name}",
            Content = content
        };

        Context.GeneratedContents.Add(generatedContent);
        await Context.SaveChangesAsync();

        Logger.LogInformation("Created practice test for test {TestId}", testId);

        return generatedContent;
    }
}
