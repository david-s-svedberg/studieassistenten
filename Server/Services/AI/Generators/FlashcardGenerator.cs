using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Services.AI.Abstractions;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;
using System.Text.Json;

namespace StudieAssistenten.Server.Services.AI.Generators;

public interface IFlashcardGenerator
{
    Task<GeneratedContent> GenerateAsync(GenerateContentRequestDto request);
}

public class FlashcardGenerator : BaseContentGenerator, IFlashcardGenerator
{
    private readonly AiProviderFactory _aiProviderFactory;

    public FlashcardGenerator(
        AiProviderFactory aiProviderFactory,
        ApplicationDbContext context,
        IRateLimitingService rateLimitingService,
        IConfiguration configuration,
        ILogger<FlashcardGenerator> logger)
        : base(context, rateLimitingService, configuration, logger)
    {
        _aiProviderFactory = aiProviderFactory;
    }

    public async Task<GeneratedContent> GenerateAsync(GenerateContentRequestDto request)
    {
        await CheckRateLimitAsync();

        // Get test with all documents
        var test = await Context.Tests
            .Include(t => t.Documents)
            .FirstOrDefaultAsync(t => t.Id == request.TestId);

        if (test == null)
        {
            throw new InvalidOperationException($"Test {request.TestId} not found");
        }

        if (!test.Documents.Any())
        {
            throw new InvalidOperationException($"Test {request.TestId} has no documents");
        }

        // Combine text from all documents
        var combinedText = string.Join("\n\n--- Next Document ---\n\n",
            test.Documents
                .Where(d => !string.IsNullOrWhiteSpace(d.ExtractedText))
                .Select(d => $"Document: {d.FileName}\n{d.ExtractedText}"));

        if (string.IsNullOrWhiteSpace(combinedText))
        {
            throw new InvalidOperationException($"No text content available in test {request.TestId} documents");
        }

        Logger.LogInformation("Generating flashcards for test {TestId} with {DocumentCount} documents",
            request.TestId, test.Documents.Count);
        Logger.LogInformation("Processing combined text length: {TextLength} characters", combinedText.Length);

        // Determine card count based on options
        var cardCount = request.NumberOfCards.HasValue
            ? $"{request.NumberOfCards.Value} flashcards"
            : "10-15 flashcards";

        // Determine difficulty instruction
        var difficultyInstruction = request.DifficultyLevel switch
        {
            "Basic" => "Focus on fundamental concepts. Keep questions and answers simple and clear.",
            "Intermediate" => "Include moderate complexity. Mix basic and advanced concepts.",
            "Advanced" => "Challenge students with complex questions requiring deep understanding.",
            _ => "Include a variety of difficulty levels from basic to advanced." // "Mixed" or null
        };

        var systemPrompt = $@"You are an educational assistant that creates flashcards from study materials.
Create flashcards in Swedish that help students learn the key concepts.
Each flashcard should have a clear question and a concise answer.
{difficultyInstruction}
Format your response as a JSON array of objects with 'question' and 'answer' properties.
Example: [{{""question"": ""Vad är fotosyntesen?"", ""answer"": ""En process där växter omvandlar ljusenergi till kemisk energi.""}}]";

        var userPrompt = $@"Create {cardCount} from the following study material:

{combinedText}

{(string.IsNullOrWhiteSpace(request.TeacherInstructions) ? "" : $"\nAdditional instructions: {request.TeacherInstructions}")}";

        var aiRequest = new AiRequest
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Temperature = 0.7m,
            EnableCaching = true
        };

        var provider = _aiProviderFactory.GetProvider();
        var response = await provider.SendMessageAsync(aiRequest);

        var content = response.Content;
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
            TestId = request.TestId,
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

        Logger.LogInformation("Created {Count} flashcards for test {TestId}", flashcardsData.Count, request.TestId);

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
