namespace StudieAssistenten.Shared.DTOs;

public class CreateTestRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
}
