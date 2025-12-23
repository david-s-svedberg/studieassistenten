namespace StudieAssistenten.Shared.Models;

/// <summary>
/// Represents a single flashcard
/// </summary>
public class Flashcard
{
    public int Id { get; set; }
    
    public int GeneratedContentId { get; set; }
    
    public string Question { get; set; } = string.Empty;
    
    public string Answer { get; set; } = string.Empty;
    
    public int Order { get; set; }
}
