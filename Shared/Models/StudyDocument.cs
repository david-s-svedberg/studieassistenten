namespace StudieAssistenten.Shared.Models;

/// <summary>
/// Represents an uploaded study material document
/// </summary>
public class StudyDocument
{
    public int Id { get; set; }
    
    public string FileName { get; set; } = string.Empty;
    
    public string? OriginalFilePath { get; set; }
    
    public long FileSizeBytes { get; set; }
    
    public string ContentType { get; set; } = string.Empty;
    
    public DateTime UploadedAt { get; set; }
    
    public Enums.DocumentStatus Status { get; set; }
    
    /// <summary>
    /// Extracted text from OCR or direct text upload
    /// </summary>
    public string? ExtractedText { get; set; }
    
    /// <summary>
    /// Any additional instructions from the teacher
    /// </summary>
    public string? TeacherInstructions { get; set; }
    
    public ICollection<GeneratedContent> GeneratedContents { get; set; } = new List<GeneratedContent>();
}
