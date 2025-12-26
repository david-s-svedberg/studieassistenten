using Anthropic.SDK.Messaging;
using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services.AI.Generators;

public interface ISummaryGenerator
{
    Task<GeneratedContent> GenerateAsync(GenerateContentRequestDto request);
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

        Logger.LogInformation("Generating summary for test {TestId} with {DocumentCount} documents",
            request.TestId, test.Documents.Count);

        // Determine length instruction
        var lengthInstruction = request.SummaryLength switch
        {
            "Brief" => "Create a brief summary focusing on critical points (1-2 pages).",
            "Detailed" => "Create a comprehensive summary covering all concepts (4-6 pages).",
            _ => "Create a balanced summary of key concepts (2-3 pages)." // "Standard" or null
        };

        // Determine format instruction
        var formatInstruction = request.SummaryFormat switch
        {
            "Paragraphs" => "Use well-structured paragraphs with clear topic sentences.",
            "Outline" => "Use a hierarchical outline format with numbered sections and subsections.",
            _ => "Use bullet points organized under clear headings." // "Bullets" or null
        };

        var systemPrompt = $@"You are an educational assistant that creates summaries of study materials.
Create a clear, structured summary in Swedish that captures the key concepts and important details.
{formatInstruction}
{lengthInstruction}
Focus on what students need to know for studying and test preparation.";

        var userPrompt = $@"Create a summary of the following study material:

{combinedText}

{(string.IsNullOrWhiteSpace(request.TeacherInstructions) ? "" : $"\nAdditional instructions: {request.TeacherInstructions}")}";

        var response = await _apiClient.SendMessageAsync(systemPrompt, userPrompt, temperature: 0.5m);

        var content = (response.Content.FirstOrDefault() as TextContent)?.Text ?? string.Empty;

        var generatedContent = new GeneratedContent
        {
            TestId = request.TestId,
            StudyDocumentId = null, // Generated from all documents in test
            ProcessingType = ProcessingType.Summary,
            GeneratedAt = DateTime.UtcNow,
            Title = $"Summary - {test.Name}",
            Content = content
        };

        Context.GeneratedContents.Add(generatedContent);
        await Context.SaveChangesAsync();

        Logger.LogInformation("Created summary for test {TestId}", request.TestId);

        return generatedContent;
    }
}
