using System;
using System.Collections.Generic;

namespace StudieAssistenten.Shared.DTOs;

public class TestDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int DocumentCount { get; set; }
    public int TotalCharacters { get; set; }
    public bool HasGeneratedContent { get; set; }
    public List<DocumentDto> Documents { get; set; } = new();
}
