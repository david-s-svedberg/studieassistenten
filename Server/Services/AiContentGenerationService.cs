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
    Task<string> SuggestTestNameAsync(int testId);
}

public class AiContentGenerationService : IAiContentGenerationService
{
    private readonly AnthropicClient _anthropicClient;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AiContentGenerationService> _logger;
    private readonly IRateLimitingService _rateLimitingService;
    private readonly IConfiguration _configuration;
    private readonly string _model;
    private readonly int _maxTokens;

    public AiContentGenerationService(
        IConfiguration configuration,
        ApplicationDbContext context,
        ILogger<AiContentGenerationService> logger,
        IRateLimitingService rateLimitingService)
    {
        _context = context;
        _logger = logger;
        _rateLimitingService = rateLimitingService;
        _configuration = configuration;

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
        // Check rate limit before making API call
        if (_configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true))
        {
            var canMakeRequest = await _rateLimitingService.CanMakeRequestAsync();
            if (!canMakeRequest)
            {
                var usage = await _rateLimitingService.GetTodayUsageAsync();
                var limit = _rateLimitingService.GetDailyTokenLimit();
                throw new InvalidOperationException(
                    $"Daily token limit exceeded. Used: {usage.TotalTokens:N0}/{limit:N0} tokens. Please try again tomorrow.");
            }
        }

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
        _logger.LogInformation("Using model: {Model}, MaxTokens: {MaxTokens}", _model, _maxTokens);
        _logger.LogInformation("Processing document with text length: {TextLength} characters", document.ExtractedText?.Length ?? 0);

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

        _logger.LogInformation("Calling Anthropic API with model: {Model}", parameters.Model);

        MessageResponse response;
        try
        {
            response = await _anthropicClient.Messages.GetClaudeMessageAsync(parameters);
            _logger.LogInformation("API call successful, response ID: {Id}", response.Id);

            // Record token usage
            if (_configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true))
            {
                await _rateLimitingService.RecordUsageAsync(
                    response.Usage.InputTokens,
                    response.Usage.OutputTokens);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic API call failed. Model: {Model}, Error: {ErrorMessage}", _model, ex.Message);
            throw;
        }
        var content = (response.Content.FirstOrDefault() as TextContent)?.Text ?? string.Empty;
        _logger.LogInformation("Received flashcards response: {Length} characters", content.Length);

        // Remove markdown code fences if present
        content = content.Trim();
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
        content = content.Trim();

        // Note: DO NOT log actual content as it may contain sensitive user data
        _logger.LogInformation("Cleaned and validated JSON response: {Length} characters", content.Length);

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
        // Check rate limit before making API call
        if (_configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true))
        {
            var canMakeRequest = await _rateLimitingService.CanMakeRequestAsync();
            if (!canMakeRequest)
            {
                var usage = await _rateLimitingService.GetTodayUsageAsync();
                var limit = _rateLimitingService.GetDailyTokenLimit();
                throw new InvalidOperationException(
                    $"Daily token limit exceeded. Used: {usage.TotalTokens:N0}/{limit:N0} tokens. Please try again tomorrow.");
            }
        }

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

        // Record token usage
        if (_configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true))
        {
            await _rateLimitingService.RecordUsageAsync(
                response.Usage.InputTokens,
                response.Usage.OutputTokens);
        }

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
        // Check rate limit before making API call
        if (_configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true))
        {
            var canMakeRequest = await _rateLimitingService.CanMakeRequestAsync();
            if (!canMakeRequest)
            {
                var usage = await _rateLimitingService.GetTodayUsageAsync();
                var limit = _rateLimitingService.GetDailyTokenLimit();
                throw new InvalidOperationException(
                    $"Daily token limit exceeded. Used: {usage.TotalTokens:N0}/{limit:N0} tokens. Please try again tomorrow.");
            }
        }

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

        // Record token usage
        if (_configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true))
        {
            await _rateLimitingService.RecordUsageAsync(
                response.Usage.InputTokens,
                response.Usage.OutputTokens);
        }

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

    public async Task<string> SuggestTestNameAsync(int testId)
    {
        // Check rate limit before making API call
        if (_configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true))
        {
            var canMakeRequest = await _rateLimitingService.CanMakeRequestAsync();
            if (!canMakeRequest)
            {
                _logger.LogWarning("Daily token limit exceeded, returning default test name");
                return $"Test - {DateTime.Now:yyyy-MM-dd}";
            }
        }

        // Get all documents for this test
        var documents = await _context.StudyDocuments
            .Where(d => d.TestId == testId && d.ExtractedText != null)
            .ToListAsync();

        if (!documents.Any())
        {
            _logger.LogWarning("No documents with extracted text found for test {TestId}", testId);
            return $"Test - {DateTime.Now:yyyy-MM-dd}";
        }

        // Combine text from all documents (limit to avoid token limits)
        var combinedText = string.Join("\n\n", documents
            .Select(d => d.ExtractedText?.Substring(0, Math.Min(d.ExtractedText.Length, 1000)))
            .Where(t => !string.IsNullOrWhiteSpace(t)));

        if (string.IsNullOrWhiteSpace(combinedText))
        {
            return $"Test - {DateTime.Now:yyyy-MM-dd}";
        }

        var systemPrompt = @"You are an educational assistant that creates concise, descriptive test names.
Based on the content provided, suggest a short, clear test name in Swedish (max 50 characters).
The name should indicate the subject or topic being covered.
Respond with ONLY the test name, nothing else.
Examples: 'Fotosyntesen och Cellbiologi', 'Svenska Grammatik - Verb', 'Andra Världskriget 1939-1945'";

        var userPrompt = $@"Based on this study material, suggest a concise test name (max 50 characters):

{combinedText}";

        _logger.LogInformation("Suggesting test name for test {TestId}", testId);

        var messages = new List<Message>
        {
            new Message(RoleType.User, userPrompt)
        };

        var parameters = new MessageParameters
        {
            Messages = messages,
            MaxTokens = 100, // Short response needed
            Model = _model,
            Stream = false,
            Temperature = 0.7m,
            System = new List<SystemMessage> { new SystemMessage(systemPrompt) }
        };

        try
        {
            var response = await _anthropicClient.Messages.GetClaudeMessageAsync(parameters);

            // Record token usage
            if (_configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true))
            {
                await _rateLimitingService.RecordUsageAsync(
                    response.Usage.InputTokens,
                    response.Usage.OutputTokens);
            }

            var suggestedName = (response.Content.FirstOrDefault() as TextContent)?.Text?.Trim() ?? string.Empty;

            // Remove quotes if present
            suggestedName = suggestedName.Trim('"', '\'');

            // Limit length
            if (suggestedName.Length > 50)
            {
                suggestedName = suggestedName.Substring(0, 47) + "...";
            }

            _logger.LogInformation("Suggested test name for test {TestId}: {Name}", testId, suggestedName);

            return string.IsNullOrWhiteSpace(suggestedName)
                ? $"Test - {DateTime.Now:yyyy-MM-dd}"
                : suggestedName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting test name for test {TestId}", testId);
            return $"Test - {DateTime.Now:yyyy-MM-dd}";
        }
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
