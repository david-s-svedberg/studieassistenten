namespace StudieAssistenten.Shared.Enums;

/// <summary>
/// Status of an uploaded document
/// </summary>
public enum DocumentStatus
{
    /// <summary>
    /// Document has been uploaded but not yet processed
    /// </summary>
    Uploaded,
    
    /// <summary>
    /// OCR is being performed on the document
    /// </summary>
    OcrInProgress,
    
    /// <summary>
    /// OCR has completed successfully
    /// </summary>
    OcrCompleted,
    
    /// <summary>
    /// OCR failed
    /// </summary>
    OcrFailed,
    
    /// <summary>
    /// AI processing is in progress
    /// </summary>
    ProcessingInProgress,
    
    /// <summary>
    /// AI processing completed successfully
    /// </summary>
    ProcessingCompleted,
    
    /// <summary>
    /// AI processing failed
    /// </summary>
    ProcessingFailed
}
