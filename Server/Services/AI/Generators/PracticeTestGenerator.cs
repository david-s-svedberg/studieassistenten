using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Services.AI.Abstractions;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;

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
Create a practice test in Swedish with multiple choice questions, short answer questions, or essay questions.
Make the questions challenging but fair, covering the key concepts from the material.

Format your response using markdown:
- Use **bold** for question numbers and important terms
- Use numbered lists (1., 2., etc.) for questions
- Use lettered lists (A), B), C), D)) for multiple choice options
- Use ## headings for sections (Questions, Answer Key)
- Use bullet points (-) in explanations when needed
- Use tables (| column | column |) for structured answer explanations or data";

        var userPrompt = $@"Create a practice test with {questionCount} from the following study material:

{combinedText}

{questionTypesInstruction}
Provide an answer key at the end. {explanationsNote}

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

        var generatedContent = new GeneratedContent
        {
            TestId = request.TestId,
            StudyDocumentId = null, // Generated from all documents in test
            ProcessingType = ProcessingType.PracticeTest,
            GeneratedAt = DateTime.UtcNow,
            Title = $"Practice Test - {test.Name}",
            Content = content
        };

        Context.GeneratedContents.Add(generatedContent);
        await Context.SaveChangesAsync();

        Logger.LogInformation("Created practice test for test {TestId}", request.TestId);

        return generatedContent;
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
