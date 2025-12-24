namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// DTO for uploading a document
/// </summary>
public class DocumentUploadDto
{
    public string FileName { get; set; } = string.Empty;
    
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSizeBytes { get; set; }
    
    public int? TestId { get; set; }
}
