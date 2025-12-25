using StudieAssistenten.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// DTO for requesting content generation
/// </summary>
public class GenerateContentRequestDto
{
    public int DocumentId { get; set; }

    public ProcessingType ProcessingType { get; set; }

    /// <summary>
    /// Optional: Number of flashcards or questions to generate
    /// </summary>
    [Range(1, 100, ErrorMessage = "Number of items must be between 1 and 100")]
    public int? NumberOfItems { get; set; }

    /// <summary>
    /// Optional: Additional instructions for content generation (teacher/student notes)
    /// Max 5000 characters to prevent resource exhaustion
    /// </summary>
    [MaxLength(5000, ErrorMessage = "Additional instructions cannot exceed 5000 characters")]
    public string? AdditionalInstructions { get; set; }

    /// <summary>
    /// Optional: Teacher-specific instructions
    /// Max 5000 characters to prevent resource exhaustion
    /// </summary>
    [MaxLength(5000, ErrorMessage = "Teacher instructions cannot exceed 5000 characters")]
    public string? TeacherInstructions { get; set; }
}
