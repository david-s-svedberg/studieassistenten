using StudieAssistenten.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// DTO for requesting content generation from a test (all documents combined)
/// </summary>
public class GenerateContentRequestDto
{
    public int TestId { get; set; }

    public ProcessingType ProcessingType { get; set; }

    /// <summary>
    /// Optional: Teacher-specific instructions for content generation
    /// Max 5000 characters to prevent resource exhaustion
    /// </summary>
    [MaxLength(5000, ErrorMessage = "Teacher instructions cannot exceed 5000 characters")]
    public string? TeacherInstructions { get; set; }

    // Flashcard options
    /// <summary>
    /// Optional: Number of flashcards to generate. If null, AI decides based on content.
    /// </summary>
    public int? NumberOfCards { get; set; }

    /// <summary>
    /// Optional: Difficulty level for flashcards. Options: "Basic", "Intermediate", "Advanced", "Mixed"
    /// </summary>
    public string? DifficultyLevel { get; set; }

    // Practice test options
    /// <summary>
    /// Optional: Number of questions to generate. If null, AI decides based on content.
    /// </summary>
    public int? NumberOfQuestions { get; set; }

    /// <summary>
    /// Optional: List of question types to include. Options: "MultipleChoice", "TrueFalse", "ShortAnswer", "Essay", "Mixed"
    /// </summary>
    public List<string>? QuestionTypes { get; set; }

    /// <summary>
    /// Optional: Whether to include detailed answer explanations in practice tests. Default: true
    /// </summary>
    public bool IncludeAnswerExplanations { get; set; } = true;

    // Summary options
    /// <summary>
    /// Optional: Length of summary. Options: "Brief", "Standard", "Detailed"
    /// </summary>
    public string? SummaryLength { get; set; }

    /// <summary>
    /// Optional: Format style for summary. Options: "Bullets", "Paragraphs", "Outline"
    /// </summary>
    public string? SummaryFormat { get; set; }
}
