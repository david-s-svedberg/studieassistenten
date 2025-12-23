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
    /// Optional: Additional parameters for content generation
    /// </summary>
    public string? AdditionalInstructions { get; set; }
}
