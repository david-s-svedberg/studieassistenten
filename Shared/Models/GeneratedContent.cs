namespace StudieAssistenten.Shared.Models;

/// <summary>
/// Represents content generated from a test (flashcards, tests, or summaries)
/// Generated from all documents in the test, not individual documents
/// </summary>
public class GeneratedContent
{
    public int Id { get; set; }

    // Generated content belongs to a test (required)
    public int TestId { get; set; }

    public Test? Test { get; set; }

    // Legacy: Previously generated content was per-document (now nullable)
    public int? StudyDocumentId { get; set; }

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
    
    /// <summary>
    /// Collection of flashcards if ProcessingType is Flashcards
    /// </summary>
    public ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();
}
