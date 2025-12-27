namespace StudieAssistenten.Shared.Models;

/// <summary>
/// Represents a single practice test question
/// </summary>
public class PracticeQuestion
{
    public int Id { get; set; }

    public int GeneratedContentId { get; set; }

    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized list of answer options for multiple choice questions
    /// </summary>
    public string OptionsJson { get; set; } = string.Empty;

    public string CorrectAnswer { get; set; } = string.Empty;

    public string? Explanation { get; set; }

    public int Order { get; set; }
}
