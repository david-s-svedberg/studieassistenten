using Anthropic.SDK.Messaging;
using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;
using System.Text.Json;

namespace StudieAssistenten.Server.Services.AI.Generators;

public interface IFlashcardGenerator
{
    Task<GeneratedContent> GenerateAsync(int testId, string? teacherInstructions = null);
}

public class FlashcardGenerator : BaseContentGenerator, IFlashcardGenerator
{
    private readonly IAnthropicApiClient _apiClient;

    public FlashcardGenerator(
        IAnthropicApiClient apiClient,
        ApplicationDbContext context,
        IRateLimitingService rateLimitingService,
        IConfiguration configuration,
        ILogger<FlashcardGenerator> logger)
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

        Logger.LogInformation("Generating flashcards for test {TestId} with {DocumentCount} documents",
            testId, test.Documents.Count);
        Logger.LogInformation("Processing combined text length: {TextLength} characters", combinedText.Length);

        var systemPrompt = @"You are an educational assistant that creates flashcards from study materials.
Create flashcards in Swedish that help students learn the key concepts.
Each flashcard should have a clear question and a concise answer.
Format your response as a JSON array of objects with 'question' and 'answer' properties.
Example: [{""question"": ""Vad är fotosyntesen?"", ""answer"": ""En process där växter omvandlar ljusenergi till kemisk energi.""}]";

        var userPrompt = $@"Create 10-15 flashcards from the following study material:

{combinedText}

{(string.IsNullOrWhiteSpace(teacherInstructions) ? "" : $"\nAdditional instructions: {teacherInstructions}")}";

        var response = await _apiClient.SendMessageAsync(systemPrompt, userPrompt, temperature: 0.7m);

        var content = (response.Content.FirstOrDefault() as TextContent)?.Text ?? string.Empty;
        Logger.LogInformation("Received flashcards response: {Length} characters", content.Length);

        // Clean JSON response (remove markdown code fences if present)
        content = CleanJsonResponse(content);

        // Parse the JSON response
        var flashcardsData = JsonSerializer.Deserialize<List<FlashcardData>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (flashcardsData == null || flashcardsData.Count == 0)
        {
            throw new InvalidOperationException("Failed to parse flashcards from AI response");
        }

        // Create GeneratedContent with flashcards (belongs to test, not individual document)
        var generatedContent = new GeneratedContent
        {
            TestId = testId,
            StudyDocumentId = null, // Generated from all documents in test
            ProcessingType = ProcessingType.Flashcards,
            GeneratedAt = DateTime.UtcNow,
            Title = $"Flashcards - {test.Name}",
            Content = content,
            Flashcards = flashcardsData.Select((fc, index) => new Flashcard
            {
                Question = fc.Question,
                Answer = fc.Answer,
                Order = index
            }).ToList()
        };

        Context.GeneratedContents.Add(generatedContent);
        await Context.SaveChangesAsync();

        Logger.LogInformation("Created {Count} flashcards for test {TestId}", flashcardsData.Count, testId);

        return generatedContent;
    }

    private static string CleanJsonResponse(string content)
    {
        content = content.Trim();

        // Remove markdown code fences if present
        if (content.StartsWith("```json"))
        {
            content = content.Substring(7); // Remove ```json
        }
        else if (content.StartsWith("```"))
        {
            content = content.Substring(3); // Remove ```
        }

        if (content.EndsWith("```"))
        {
            content = content.Substring(0, content.Length - 3); // Remove trailing ```
        }

        return content.Trim();
    }

    private class FlashcardData
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}
