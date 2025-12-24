using System;
using System.Collections.Generic;

namespace StudieAssistenten.Shared.Models;

public class Test
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public List<StudyDocument> Documents { get; set; } = new();
    public List<GeneratedContent> GeneratedContents { get; set; } = new();
}
