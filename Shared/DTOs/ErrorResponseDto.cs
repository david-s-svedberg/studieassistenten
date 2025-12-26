namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// Standardized error response for API endpoints
/// </summary>
public class ErrorResponseDto
{
    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Detailed error information (only in development)
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Optional: Error code for client-side handling
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ErrorResponseDto() { }

    public ErrorResponseDto(string message, string? detail = null, string? errorCode = null)
    {
        Message = message;
        Detail = detail;
        ErrorCode = errorCode;
    }
}
