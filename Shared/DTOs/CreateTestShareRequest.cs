namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// Request DTO for creating a new test share
/// </summary>
public class CreateTestShareRequest
{
    public int TestId { get; set; }
    public string SharedWithEmail { get; set; } = string.Empty;
}
