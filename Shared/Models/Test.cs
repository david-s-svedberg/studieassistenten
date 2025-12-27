using System;
using System.Collections.Generic;

namespace StudieAssistenten.Shared.Models;

public class Test
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }

    // User ownership (required)
    public string UserId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ApplicationUser? User { get; set; }
    public List<StudyDocument> Documents { get; set; } = new();
    public List<GeneratedContent> GeneratedContents { get; set; } = new();

    /// <summary>
    /// Share relationships for this test.
    /// A test can be shared with multiple users (one-to-many).
    /// </summary>
    public List<TestShare> Shares { get; set; } = new();
}
