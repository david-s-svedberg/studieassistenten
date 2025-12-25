using System.ComponentModel.DataAnnotations;

namespace StudieAssistenten.Shared.DTOs;

public class CreateTestRequest
{
    [Required(ErrorMessage = "Test name is required")]
    [MaxLength(200, ErrorMessage = "Test name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    [MaxLength(5000, ErrorMessage = "Instructions cannot exceed 5000 characters")]
    public string? Instructions { get; set; }
}
