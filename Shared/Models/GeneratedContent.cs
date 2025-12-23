namespace StudieAssistenten.Shared.Models;

/// <summary>
/// Represents content generated from a study document (flashcards, tests, or summaries)
/// </summary>
public class GeneratedContent
{
    public int Id { get; set; }
    
    public int StudyDocumentId { get; set; }
    
    public StudyDocument? StudyDocument { get; set; }
    
    public Enums.ProcessingType ProcessingType { get; set; }
    
    public DateTime GeneratedAt { get; set; }
    
    /// <summary>
    /// The generated content (JSON or formatted text)
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional title or subject for this content
    /// </summary>
    public string? Title { get; set; }
}
