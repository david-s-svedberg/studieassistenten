using System.ComponentModel.DataAnnotations;

namespace StudieAssistenten.Shared.DTOs;

/// <summary>
/// Pagination parameters for list requests
/// </summary>
public class PaginationRequestDto
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Calculate the number of items to skip
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;
}
