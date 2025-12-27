using StudieAssistenten.Shared.Enums;

namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// DTO for test share information
/// </summary>
public class TestShareDto
{
    public int Id { get; set; }
    public int TestId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public UserDto Owner { get; set; } = new();
    public UserDto SharedWith { get; set; } = new();
    public DateTime SharedAt { get; set; }
    public SharePermission Permission { get; set; }
    public bool IsActive { get; set; }
}
