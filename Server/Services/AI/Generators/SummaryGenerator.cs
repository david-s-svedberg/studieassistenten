using Anthropic.SDK.Messaging;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services.AI.Generators;

public interface ISummaryGenerator
{
    Task<GeneratedContent> GenerateAsync(int documentId, string? teacherInstructions = null);
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

    public async Task<GeneratedContent> GenerateAsync(int documentId, string? teacherInstructions = null)
    {
        await CheckRateLimitAsync();

        var document = await GetDocumentWithTextAsync(documentId);

        Logger.LogInformation("Generating summary for document {DocumentId}", documentId);

        var systemPrompt = @"You are an educational assistant that creates concise summaries of study materials.
Create a clear, structured summary in Swedish that captures the key concepts and important details.
Use bullet points, headings, and formatting to make the summary easy to scan and understand.
Focus on what students need to know for studying and test preparation.";

        var userPrompt = $@"Create a comprehensive but concise summary of the following study material:

{document.ExtractedText}

{(string.IsNullOrWhiteSpace(teacherInstructions) ? "" : $"\nAdditional instructions: {teacherInstructions}")}";

        var response = await _apiClient.SendMessageAsync(systemPrompt, userPrompt, temperature: 0.5m);

        var content = (response.Content.FirstOrDefault() as TextContent)?.Text ?? string.Empty;

        var generatedContent = new GeneratedContent
        {
            StudyDocumentId = documentId,
            ProcessingType = ProcessingType.Summary,
            GeneratedAt = DateTime.UtcNow,
            Title = $"Summary - {document.FileName}",
            Content = content
        };

        Context.GeneratedContents.Add(generatedContent);
        await Context.SaveChangesAsync();

        Logger.LogInformation("Created summary for document {DocumentId}", documentId);

        return generatedContent;
    }
}
