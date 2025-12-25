using StudieAssistenten.Shared.Enums;

namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// Lightweight DTO for document lists - excludes extracted text
/// </summary>
public class DocumentSummaryDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public DocumentStatus Status { get; set; }
    public int? TestId { get; set; }
}
