using StudieAssistenten.Shared.Enums;

namespace StudieAssistenten.Shared.DTOs;

public class GeneratedContentDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ProcessingType ProcessingType { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string Content { get; set; } = string.Empty;
    public int FlashcardsCount { get; set; }
    public List<FlashcardDto> Flashcards { get; set; } = new();
    public List<PracticeQuestionDto> PracticeQuestions { get; set; } = new();
}

public class FlashcardDto
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class PracticeQuestionDto
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public int Order { get; set; }
}
