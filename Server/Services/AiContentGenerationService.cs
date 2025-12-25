using StudieAssistenten.Server.Services.AI;
using StudieAssistenten.Server.Services.AI.Generators;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services;

public interface IAiContentGenerationService
{
    Task<GeneratedContent> GenerateFlashcardsAsync(int documentId, string? teacherInstructions = null);
    Task<GeneratedContent> GeneratePracticeTestAsync(int documentId, string? teacherInstructions = null);
    Task<GeneratedContent> GenerateSummaryAsync(int documentId, string? teacherInstructions = null);
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

    public AiContentGenerationService(
        IFlashcardGenerator flashcardGenerator,
        IPracticeTestGenerator practiceTestGenerator,
        ISummaryGenerator summaryGenerator,
        ITestNamingService testNamingService)
    {
        _flashcardGenerator = flashcardGenerator;
        _practiceTestGenerator = practiceTestGenerator;
        _summaryGenerator = summaryGenerator;
        _testNamingService = testNamingService;
    }

    public async Task<GeneratedContent> GenerateFlashcardsAsync(int documentId, string? teacherInstructions = null)
    {
        return await _flashcardGenerator.GenerateAsync(documentId, teacherInstructions);
    }

    public async Task<GeneratedContent> GeneratePracticeTestAsync(int documentId, string? teacherInstructions = null)
    {
        return await _practiceTestGenerator.GenerateAsync(documentId, teacherInstructions);
    }

    public async Task<GeneratedContent> GenerateSummaryAsync(int documentId, string? teacherInstructions = null)
    {
        return await _summaryGenerator.GenerateAsync(documentId, teacherInstructions);
    }

    public async Task<string> SuggestTestNameAsync(int testId)
    {
        return await _testNamingService.SuggestTestNameAsync(testId);
    }
}
