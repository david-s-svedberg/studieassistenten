using StudieAssistenten.Shared.Enums;

namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// Detailed DTO for individual document view - includes extracted text
/// </summary>
public class DocumentDetailDto
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
