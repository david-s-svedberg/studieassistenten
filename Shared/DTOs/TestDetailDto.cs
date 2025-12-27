namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// Detailed DTO for individual test view - includes document summaries
/// </summary>
public class TestDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int DocumentCount { get; set; }
    public int TotalCharacters { get; set; }
    public bool HasGeneratedContent { get; set; }
    public bool IsOwner { get; set; }
    public List<DocumentSummaryDto> Documents { get; set; } = new();
}
