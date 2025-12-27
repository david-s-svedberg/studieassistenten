namespace StudieAssistenten.Shared.Enums;

/// <summary>
/// Permission levels for shared tests
/// </summary>
public enum SharePermission
{
    /// <summary>
    /// Can view test, documents, and generated content (read-only)
    /// </summary>
    Read = 0,

    /// <summary>
    /// Read access plus ability to add comments (future feature)
    /// </summary>
    Comment = 1,

    /// <summary>
    /// Can modify test and generate content (future feature)
    /// </summary>
    Edit = 2
}
