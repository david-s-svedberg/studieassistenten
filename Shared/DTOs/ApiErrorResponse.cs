namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// Standardized error response for API endpoints
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional error code for programmatic handling
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Optional validation errors (field-level errors)
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    public ApiErrorResponse()
    {
    }

    public ApiErrorResponse(string message)
    {
        Message = message;
    }

    public ApiErrorResponse(string message, string errorCode)
    {
        Message = message;
        ErrorCode = errorCode;
    }
}
