namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// Lightweight DTO for test lists - excludes heavy document data
/// </summary>
public class TestListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int DocumentCount { get; set; }
    public int TotalCharacters { get; set; }
    public bool HasGeneratedContent { get; set; }
    public bool IsOwner { get; set; }
    public int ShareCount { get; set; }
    public UserDto? Owner { get; set; }
}
