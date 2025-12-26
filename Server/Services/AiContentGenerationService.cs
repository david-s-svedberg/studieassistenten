using Microsoft.Extensions.Logging;
using StudieAssistenten.Server.Services.AI;
using StudieAssistenten.Server.Services.AI.Generators;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services;

public interface IAiContentGenerationService
{
    Task<GeneratedContent> GenerateFlashcardsAsync(GenerateContentRequestDto request);
    Task<GeneratedContent> GeneratePracticeTestAsync(GenerateContentRequestDto request);
    Task<GeneratedContent> GenerateSummaryAsync(GenerateContentRequestDto request);
    Task<string> SuggestTestNameAsync(int testId);
}

/// <summary>
/// Coordinator service that delegates to specialized content generators.
/// </summary>
public class AiContentGenerationService : IAiContentGenerationService
{
    private readonly IFlashcardGenerator _flashcardGenerator;
    private readonly IPracticeTestGenerator _practiceTestGenerator;
    private readonly ISummaryGenerator _summaryGenerator;
    private readonly ITestNamingService _testNamingService;
    private readonly ILogger<AiContentGenerationService> _logger;

    public AiContentGenerationService(
        IFlashcardGenerator flashcardGenerator,
        IPracticeTestGenerator practiceTestGenerator,
        ISummaryGenerator summaryGenerator,
        ITestNamingService testNamingService,
        ILogger<AiContentGenerationService> logger)
    {
        _flashcardGenerator = flashcardGenerator;
        _practiceTestGenerator = practiceTestGenerator;
        _summaryGenerator = summaryGenerator;
        _testNamingService = testNamingService;
        _logger = logger;
    }

    public async Task<GeneratedContent> GenerateFlashcardsAsync(GenerateContentRequestDto request)
    {
        _logger.LogInformation("Generating flashcards for test {TestId} with {NumberOfCards} cards and {DifficultyLevel} difficulty",
            request.TestId, request.NumberOfCards ?? 0, request.DifficultyLevel ?? "Mixed");
        return await _flashcardGenerator.GenerateAsync(request);
    }

    public async Task<GeneratedContent> GeneratePracticeTestAsync(GenerateContentRequestDto request)
    {
        _logger.LogInformation("Generating practice test for test {TestId} with {NumberOfQuestions} questions",
            request.TestId, request.NumberOfQuestions ?? 0);
        return await _practiceTestGenerator.GenerateAsync(request);
    }

    public async Task<GeneratedContent> GenerateSummaryAsync(GenerateContentRequestDto request)
    {
        _logger.LogInformation("Generating summary for test {TestId} with length {SummaryLength} and format {SummaryFormat}",
            request.TestId, request.SummaryLength ?? "Standard", request.SummaryFormat ?? "Bullets");
        return await _summaryGenerator.GenerateAsync(request);
    }

    public async Task<string> SuggestTestNameAsync(int testId)
    {
        _logger.LogInformation("Suggesting test name for test {TestId}", testId);
        return await _testNamingService.SuggestTestNameAsync(testId);
    }
}
