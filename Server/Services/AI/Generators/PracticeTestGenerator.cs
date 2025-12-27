using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Services.AI.Abstractions;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;
using System.Text.Json;

namespace StudieAssistenten.Server.Services.AI.Generators;

public interface IPracticeTestGenerator
{
    Task<GeneratedContent> GenerateAsync(GenerateContentRequestDto request);
}

public class PracticeTestGenerator : BaseContentGenerator, IPracticeTestGenerator
{
    private readonly AiProviderFactory _aiProviderFactory;

    public PracticeTestGenerator(
        AiProviderFactory aiProviderFactory,
        ApplicationDbContext context,
        IRateLimitingService rateLimitingService,
        IConfiguration configuration,
        ILogger<PracticeTestGenerator> logger)
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

        Logger.LogInformation("Generating practice test for test {TestId} with {DocumentCount} documents",
            request.TestId, test.Documents.Count);

        // Determine question count based on options
        var questionCount = request.NumberOfQuestions.HasValue
            ? $"{request.NumberOfQuestions.Value} questions"
            : "5-10 questions";

        // Build question types instruction
        var questionTypesInstruction = BuildQuestionTypesInstruction(request.QuestionTypes);

        // Determine explanation note
        var explanationsNote = request.IncludeAnswerExplanations
            ? "Include detailed explanations for each answer."
            : "Provide an answer key without detailed explanations.";

        var systemPrompt = @"You are an educational assistant that creates practice tests from study materials.
Create a practice test in Swedish with multiple-choice questions.
Make the questions challenging but fair, covering the key concepts from the material.

CRITICAL: Return ONLY a JSON array of question objects, with no additional text or markdown.
Each question object must have this exact structure:
{
  ""question"": ""The question text in Swedish"",
  ""options"": [""Option A"", ""Option B"", ""Option C"", ""Option D""],
  ""correctAnswer"": ""The correct option text (must match one of the options exactly)"",
  ""explanation"": ""Explanation of why this is the correct answer (optional, can be null)""
}

Example response format:
[
  {
    ""question"": ""Vad är huvudstaden i Sverige?"",
    ""options"": [""Oslo"", ""Stockholm"", ""Köpenhamn"", ""Helsingfors""],
    ""correctAnswer"": ""Stockholm"",
    ""explanation"": ""Stockholm är Sveriges huvudstad sedan 1634.""
  }
]";

        var userPrompt = $@"Create a multiple-choice practice test with {questionCount} from the following study material.
Each question should have 4 options.
{explanationsNote}

Study material:
{combinedText}

{(string.IsNullOrWhiteSpace(request.TeacherInstructions) ? "" : $"\nAdditional instructions: {request.TeacherInstructions}")}

Return ONLY the JSON array, no markdown formatting or additional text.";

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
        Logger.LogInformation("Received practice test response: {Length} characters", content.Length);

        // Clean JSON response (remove markdown code fences if present)
        content = CleanJsonResponse(content);

        // Parse the JSON response
        var questionsData = JsonSerializer.Deserialize<List<PracticeQuestionData>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (questionsData == null || questionsData.Count == 0)
        {
            throw new InvalidOperationException("Failed to parse practice questions from AI response");
        }

        // Create GeneratedContent with practice questions
        var generatedContent = new GeneratedContent
        {
            TestId = request.TestId,
            StudyDocumentId = null, // Generated from all documents in test
            ProcessingType = ProcessingType.PracticeTest,
            GeneratedAt = DateTime.UtcNow,
            Title = $"Practice Test - {test.Name}",
            Content = content,
            PracticeQuestions = questionsData.Select((q, index) => new PracticeQuestion
            {
                Question = q.Question,
                OptionsJson = JsonSerializer.Serialize(q.Options),
                CorrectAnswer = q.CorrectAnswer,
                Explanation = q.Explanation,
                Order = index
            }).ToList()
        };

        Context.GeneratedContents.Add(generatedContent);
        await Context.SaveChangesAsync();

        Logger.LogInformation("Created {Count} practice questions for test {TestId}", questionsData.Count, request.TestId);

        return generatedContent;
    }

    private static string CleanJsonResponse(string content)
    {
        content = content.Trim();

        // Remove markdown code fences if present
        if (content.StartsWith("```"))
        {
            var lines = content.Split('\n');
            content = string.Join('\n', lines.Skip(1).Take(lines.Length - 2));
        }

        return content.Trim();
    }

    private string BuildQuestionTypesInstruction(List<string>? types)
    {
        if (types == null || !types.Any() || types.Contains("Mixed"))
            return "Include a mix of question types (multiple choice, true/false, short answer, essay).";

        var typeDescriptions = new List<string>();
        if (types.Contains("MultipleChoice")) typeDescriptions.Add("multiple choice");
        if (types.Contains("TrueFalse")) typeDescriptions.Add("true/false");
        if (types.Contains("ShortAnswer")) typeDescriptions.Add("short answer");
        if (types.Contains("Essay")) typeDescriptions.Add("essay");

        return $"Include only these question types: {string.Join(", ", typeDescriptions)}.";
    }
}

/// <summary>
/// Temporary data structure for deserializing practice questions from AI response
/// </summary>
internal class PracticeQuestionData
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
}
