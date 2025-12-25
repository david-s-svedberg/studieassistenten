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
}
