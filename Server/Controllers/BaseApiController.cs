using Microsoft.AspNetCore.Mvc;
using StudieAssistenten.Shared.Models;
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

    /// <summary>
    /// Verifies that a test belongs to the current user
    /// </summary>
    /// <param name="test">The test to verify</param>
    /// <param name="userId">The current user's ID</param>
    /// <returns>NotFoundResult if test is null or doesn't belong to user, otherwise null</returns>
    protected IActionResult? VerifyTestOwnership(Test? test, string userId)
    {
        if (test == null || test.UserId != userId)
        {
            return NotFound();
        }
        return null;
    }

    /// <summary>
    /// Verifies that a document belongs to the current user (via its test)
    /// </summary>
    /// <param name="document">The document to verify</param>
    /// <param name="userId">The current user's ID</param>
    /// <returns>NotFoundResult if document is null or doesn't belong to user, otherwise null</returns>
    protected IActionResult? VerifyDocumentOwnership(StudyDocument? document, string userId)
    {
        if (document == null || document.Test == null || document.Test.UserId != userId)
        {
            return NotFound();
        }
        return null;
    }

    /// <summary>
    /// Verifies that generated content belongs to the current user (via its test)
    /// </summary>
    /// <param name="content">The generated content to verify</param>
    /// <param name="userId">The current user's ID</param>
    /// <returns>NotFoundResult if content is null or doesn't belong to user, otherwise null</returns>
    protected IActionResult? VerifyContentOwnership(GeneratedContent? content, string userId)
    {
        if (content == null || content.Test?.UserId != userId)
        {
            return NotFound();
        }
        return null;
    }
}
