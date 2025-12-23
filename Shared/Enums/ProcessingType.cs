namespace StudieAssistenten.Shared.Enums;

/// <summary>
/// Types of processing that can be performed on uploaded materials
/// </summary>
public enum ProcessingType
{
    /// <summary>
    /// Generate flashcards in Question|Answer format
    /// </summary>
    Flashcards,
    
    /// <summary>
    /// Generate a practice test with multiple choice questions
    /// </summary>
    PracticeTest,
    
    /// <summary>
    /// Generate a condensed summary of the material
    /// </summary>
    Summary
}
