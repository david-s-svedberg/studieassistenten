using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace StudieAssistenten.Server.Controllers;

/// <summary>
/// Base controller with common functionality for all API controllers
/// </summary>
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Gets the current authenticated user's ID
    /// </summary>
    /// <returns>User ID</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if user is not authenticated</exception>
    protected string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User not authenticated");
    }

    /// <summary>
    /// Gets the current authenticated user's ID or null if not authenticated
    /// </summary>
    /// <returns>User ID or null</returns>
    protected string? GetCurrentUserIdOrNull()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
