using StudieAssistenten.Shared.Enums;

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
    public int? NumberOfItems { get; set; }
    
    /// <summary>
    /// Optional: Additional instructions for content generation (teacher/student notes)
    /// </summary>
    public string? AdditionalInstructions { get; set; }
    
    /// <summary>
    /// Optional: Teacher-specific instructions
    /// </summary>
    public string? TeacherInstructions { get; set; }
}
