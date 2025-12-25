using StudieAssistenten.Shared.Enums;

namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// DTO for document information
/// </summary>
public class DocumentDto
{
    public int Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    public DateTime UploadedAt { get; set; }

    public DocumentStatus Status { get; set; }

    public string? ExtractedText { get; set; }

    public int? TestId { get; set; }
}
