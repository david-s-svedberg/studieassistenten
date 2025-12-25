using Anthropic.SDK.Messaging;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services.AI.Generators;

public interface IPracticeTestGenerator
{
    Task<GeneratedContent> GenerateAsync(int documentId, string? teacherInstructions = null);
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

    public async Task<GeneratedContent> GenerateAsync(int documentId, string? teacherInstructions = null)
    {
        await CheckRateLimitAsync();

        var document = await GetDocumentWithTextAsync(documentId);

        Logger.LogInformation("Generating practice test for document {DocumentId}", documentId);

        var systemPrompt = @"You are an educational assistant that creates practice tests from study materials.
Create a practice test in Swedish with multiple choice questions, short answer questions, or essay questions.
Make the questions challenging but fair, covering the key concepts from the material.
Format your response as markdown with clear numbering and formatting.";

        var userPrompt = $@"Create a practice test with 5-10 questions from the following study material:

{document.ExtractedText}

Include a mix of question types (multiple choice, short answer) and provide an answer key at the end.

{(string.IsNullOrWhiteSpace(teacherInstructions) ? "" : $"\nAdditional instructions: {teacherInstructions}")}";

        var response = await _apiClient.SendMessageAsync(systemPrompt, userPrompt, temperature: 0.7m);

        var content = (response.Content.FirstOrDefault() as TextContent)?.Text ?? string.Empty;

        var generatedContent = new GeneratedContent
        {
            StudyDocumentId = documentId,
            ProcessingType = ProcessingType.PracticeTest,
            GeneratedAt = DateTime.UtcNow,
            Title = $"Practice Test - {document.FileName}",
            Content = content
        };

        Context.GeneratedContents.Add(generatedContent);
        await Context.SaveChangesAsync();

        Logger.LogInformation("Created practice test for document {DocumentId}", documentId);

        return generatedContent;
    }
}
