using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Validation;
using System.ComponentModel.DataAnnotations;

namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// DTO for requesting content generation from a test (all documents combined)
/// </summary>
public class GenerateContentRequestDto
{
    [Required(ErrorMessage = "TestId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "TestId must be a positive number")]
    public int TestId { get; set; }

    [Required(ErrorMessage = "ProcessingType is required")]
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
    /// Valid range: 1-100
    /// </summary>
    [Range(1, 100, ErrorMessage = "Number of cards must be between 1 and 100")]
    public int? NumberOfCards { get; set; }

    /// <summary>
    /// Optional: Difficulty level for flashcards. Options: "Basic", "Intermediate", "Advanced", "Mixed"
    /// </summary>
    [RegularExpression("^(Basic|Intermediate|Advanced|Mixed)$", ErrorMessage = "Difficulty level must be Basic, Intermediate, Advanced, or Mixed")]
    public string? DifficultyLevel { get; set; }

    // Practice test options
    /// <summary>
    /// Optional: Number of questions to generate. If null, AI decides based on content.
    /// Valid range: 1-50
    /// </summary>
    [Range(1, 50, ErrorMessage = "Number of questions must be between 1 and 50")]
    public int? NumberOfQuestions { get; set; }

    /// <summary>
    /// Optional: List of question types to include. Options: "MultipleChoice", "TrueFalse", "ShortAnswer", "Essay", "Mixed"
    /// Each type must be one of the valid options.
    /// </summary>
    [ValidQuestionTypes]
    public List<string>? QuestionTypes { get; set; }

    /// <summary>
    /// Optional: Whether to include detailed answer explanations in practice tests. Default: true
    /// </summary>
    public bool IncludeAnswerExplanations { get; set; } = true;

    // Summary options
    /// <summary>
    /// Optional: Length of summary. Options: "Brief", "Standard", "Detailed"
    /// </summary>
    [RegularExpression("^(Brief|Standard|Detailed)$", ErrorMessage = "Summary length must be Brief, Standard, or Detailed")]
    public string? SummaryLength { get; set; }

    /// <summary>
    /// Optional: Format style for summary. Options: "Bullets", "Paragraphs", "Outline"
    /// </summary>
    [RegularExpression("^(Bullets|Paragraphs|Outline)$", ErrorMessage = "Summary format must be Bullets, Paragraphs, or Outline")]
    public string? SummaryFormat { get; set; }
}
