using Microsoft.AspNetCore.Mvc;
using StudieAssistenten.Shared.DTOs;
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
    /// <returns>NotFoundResult if test is null, ForbidResult if doesn't belong to user, otherwise null</returns>
    protected IActionResult? VerifyTestOwnership(Test? test, string userId)
    {
        if (test == null)
        {
            return NotFound();
        }
        if (test.UserId != userId)
        {
            return Forbid();
        }
        return null;
    }

    /// <summary>
    /// Verifies that a document belongs to the current user (via its test)
    /// </summary>
    /// <param name="document">The document to verify</param>
    /// <param name="userId">The current user's ID</param>
    /// <returns>NotFoundResult if document is null, ForbidResult if doesn't belong to user, otherwise null</returns>
    protected IActionResult? VerifyDocumentOwnership(StudyDocument? document, string userId)
    {
        if (document == null || document.Test == null)
        {
            return NotFound();
        }
        if (document.Test.UserId != userId)
        {
            return Forbid();
        }
        return null;
    }

    /// <summary>
    /// Verifies that generated content belongs to the current user (via its test)
    /// </summary>
    /// <param name="content">The generated content to verify</param>
    /// <param name="userId">The current user's ID</param>
    /// <returns>NotFoundResult if content is null, ForbidResult if doesn't belong to user, otherwise null</returns>
    protected IActionResult? VerifyContentOwnership(GeneratedContent? content, string userId)
    {
        if (content == null || content.Test == null)
        {
            return NotFound();
        }
        if (content.Test.UserId != userId)
        {
            return Forbid();
        }
        return null;
    }

    /// <summary>
    /// Returns a standardized 500 Internal Server Error response
    /// </summary>
    /// <param name="message">User-friendly error message</param>
    /// <param name="detail">Detailed error information (optional, only in development)</param>
    /// <param name="errorCode">Optional error code for client handling</param>
    /// <returns>ObjectResult with 500 status code and ErrorResponseDto</returns>
    protected ObjectResult InternalServerError(string message, string? detail = null, string? errorCode = null)
    {
        var error = new ErrorResponseDto(message, detail, errorCode);
        return StatusCode(StatusCodes.Status500InternalServerError, error);
    }

    /// <summary>
    /// Returns a standardized 400 Bad Request response
    /// </summary>
    /// <param name="message">User-friendly error message</param>
    /// <param name="detail">Detailed error information (optional)</param>
    /// <param name="errorCode">Optional error code for client handling</param>
    /// <returns>BadRequestObjectResult with ErrorResponseDto</returns>
    protected BadRequestObjectResult BadRequestError(string message, string? detail = null, string? errorCode = null)
    {
        var error = new ErrorResponseDto(message, detail, errorCode);
        return BadRequest(error);
    }
}
