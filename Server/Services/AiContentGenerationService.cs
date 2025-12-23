using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace StudieAssistenten.Server.Services;

public interface IAiContentGenerationService
{
    Task<GeneratedContent> GenerateFlashcardsAsync(int documentId, string? teacherInstructions = null);
    Task<GeneratedContent> GeneratePracticeTestAsync(int documentId, string? teacherInstructions = null);
    Task<GeneratedContent> GenerateSummaryAsync(int documentId, string? teacherInstructions = null);
}

public class AiContentGenerationService : IAiContentGenerationService
{
    private readonly AnthropicClient _anthropicClient;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AiContentGenerationService> _logger;
    private readonly string _model;
    private readonly int _maxTokens;

    public AiContentGenerationService(
        IConfiguration configuration,
        ApplicationDbContext context,
        ILogger<AiContentGenerationService> logger)
    {
        _context = context;
        _logger = logger;

        var apiKey = configuration["Anthropic:ApiKey"] 
            ?? throw new InvalidOperationException("Anthropic API key not configured");
        _model = configuration["Anthropic:Model"] ?? "claude-3-5-sonnet-20241022";
        _maxTokens = int.Parse(configuration["Anthropic:MaxTokens"] ?? "4000");

        if (apiKey == "your-api-key-here" || string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Please set your Anthropic API key in appsettings.Development.json");
        }

        _anthropicClient = new AnthropicClient(new APIAuthentication(apiKey));
    }

    public async Task<GeneratedContent> GenerateFlashcardsAsync(int documentId, string? teacherInstructions = null)
    {
        var document = await GetDocumentWithTextAsync(documentId);

        var systemPrompt = @"You are an educational assistant that creates flashcards from study materials.
Create flashcards in Swedish that help students learn the key concepts.
Each flashcard should have a clear question and a concise answer.
Format your response as a JSON array of objects with 'question' and 'answer' properties.
Example: [{""question"": ""Vad är fotosyntesen?"", ""answer"": ""En process där växter omvandlar ljusenergi till kemisk energi.""}]";

        var userPrompt = $@"Create 10-15 flashcards from the following study material:

{document.ExtractedText}

{(string.IsNullOrWhiteSpace(teacherInstructions) ? "" : $"\nAdditional instructions: {teacherInstructions}")}";

        _logger.LogInformation("Generating flashcards for document {DocumentId}", documentId);

        var messages = new List<Message>
        {
            new Message(RoleType.User, userPrompt)
        };

        var parameters = new MessageParameters
        {
            Messages = messages,
            MaxTokens = _maxTokens,
            Model = _model,
            Stream = false,
            Temperature = 0.7m,
            System = new List<SystemMessage> { new SystemMessage(systemPrompt) }
        };

        var response = await _anthropicClient.Messages.GetClaudeMessageAsync(parameters);
        var content = (response.Content.FirstOrDefault() as TextContent)?.Text ?? string.Empty;
        _logger.LogInformation("Received flashcards response: {Length} characters", content.Length);

        // Parse the JSON response
        var flashcardsData = JsonSerializer.Deserialize<List<FlashcardData>>(content, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });

        if (flashcardsData == null || flashcardsData.Count == 0)
        {
            throw new InvalidOperationException("Failed to parse flashcards from AI response");
        }

        // Create GeneratedContent with flashcards
        var generatedContent = new GeneratedContent
        {
            StudyDocumentId = documentId,
            ProcessingType = ProcessingType.Flashcards,
            GeneratedAt = DateTime.UtcNow,
            Title = $"Flashcards - {document.FileName}",
            Content = content,
            Flashcards = flashcardsData.Select((fc, index) => new Flashcard
            {
                Question = fc.Question,
                Answer = fc.Answer,
                Order = index
            }).ToList()
        };

        _context.GeneratedContents.Add(generatedContent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created {Count} flashcards for document {DocumentId}", flashcardsData.Count, documentId);

        return generatedContent;
    }

    public async Task<GeneratedContent> GeneratePracticeTestAsync(int documentId, string? teacherInstructions = null)
    {
        var document = await GetDocumentWithTextAsync(documentId);

        var systemPrompt = @"You are an educational assistant that creates practice tests from study materials.
Create a practice test in Swedish with multiple choice questions, short answer questions, or essay questions.
Make the questions challenging but fair, covering the key concepts from the material.
Format your response as markdown with clear numbering and formatting.";

        var userPrompt = $@"Create a practice test with 5-10 questions from the following study material:

{document.ExtractedText}

Include a mix of question types (multiple choice, short answer) and provide an answer key at the end.

{(string.IsNullOrWhiteSpace(teacherInstructions) ? "" : $"\nAdditional instructions: {teacherInstructions}")}";

        _logger.LogInformation("Generating practice test for document {DocumentId}", documentId);

        var messages = new List<Message>
        {
            new Message(RoleType.User, userPrompt)
        };

        var parameters = new MessageParameters
        {
            Messages = messages,
            MaxTokens = _maxTokens,
            Model = _model,
            Stream = false,
            Temperature = 0.7m,
            System = new List<SystemMessage> { new SystemMessage(systemPrompt) }
        };

        var response = await _anthropicClient.Messages.GetClaudeMessageAsync(parameters);
        var content = (response.Content.FirstOrDefault() as TextContent)?.Text ?? string.Empty;

        var generatedContent = new GeneratedContent
        {
            StudyDocumentId = documentId,
            ProcessingType = ProcessingType.PracticeTest,
            GeneratedAt = DateTime.UtcNow,
            Title = $"Practice Test - {document.FileName}",
            Content = content
        };

        _context.GeneratedContents.Add(generatedContent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created practice test for document {DocumentId}", documentId);

        return generatedContent;
    }

    public async Task<GeneratedContent> GenerateSummaryAsync(int documentId, string? teacherInstructions = null)
    {
        var document = await GetDocumentWithTextAsync(documentId);

        var systemPrompt = @"You are an educational assistant that creates concise summaries of study materials.
Create a clear, structured summary in Swedish that captures the key concepts and important details.
Use bullet points, headings, and formatting to make the summary easy to scan and understand.
Focus on what students need to know for studying and test preparation.";

        var userPrompt = $@"Create a comprehensive but concise summary of the following study material:

{document.ExtractedText}

{(string.IsNullOrWhiteSpace(teacherInstructions) ? "" : $"\nAdditional instructions: {teacherInstructions}")}";

        _logger.LogInformation("Generating summary for document {DocumentId}", documentId);

        var messages = new List<Message>
        {
            new Message(RoleType.User, userPrompt)
        };

        var parameters = new MessageParameters
        {
            Messages = messages,
            MaxTokens = _maxTokens,
            Model = _model,
            Stream = false,
            Temperature = 0.5m,
            System = new List<SystemMessage> { new SystemMessage(systemPrompt) }
        };

        var response = await _anthropicClient.Messages.GetClaudeMessageAsync(parameters);
        var content = (response.Content.FirstOrDefault() as TextContent)?.Text ?? string.Empty;

        var generatedContent = new GeneratedContent
        {
            StudyDocumentId = documentId,
            ProcessingType = ProcessingType.Summary,
            GeneratedAt = DateTime.UtcNow,
            Title = $"Summary - {document.FileName}",
            Content = content
        };

        _context.GeneratedContents.Add(generatedContent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created summary for document {DocumentId}", documentId);

        return generatedContent;
    }

    private async Task<StudyDocument> GetDocumentWithTextAsync(int documentId)
    {
        var document = await _context.StudyDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            throw new InvalidOperationException($"Document {documentId} not found");
        }

        if (string.IsNullOrWhiteSpace(document.ExtractedText))
        {
            throw new InvalidOperationException($"Document {documentId} has no extracted text. Please run OCR first.");
        }

        return document;
    }

    private class FlashcardData
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}
