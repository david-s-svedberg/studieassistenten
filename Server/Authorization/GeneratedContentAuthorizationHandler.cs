using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Models;
using System.Security.Claims;

namespace StudieAssistenten.Server.Authorization;

/// <summary>
/// Authorization handler for GeneratedContent resources
/// Checks both ownership and shared access
/// </summary>
public class GeneratedContentAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, GeneratedContent>
{
    private readonly ApplicationDbContext _context;

    public GeneratedContentAuthorizationHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        GeneratedContent resource)
    {
        if (context.User == null || resource == null)
        {
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        // Check ownership - owners have full access
        if (resource.StudyDocument != null &&
            resource.StudyDocument.Test != null &&
            resource.StudyDocument.Test.UserId == userId)
        {
            context.Succeed(requirement);
            return;
        }

        // Alternatively, check via TestId if Test navigation isn't loaded
        if (resource.Test != null && resource.Test.UserId == userId)
        {
            context.Succeed(requirement);
            return;
        }

        // Check shared access - only for Read operations
        if (requirement.Name == ResourceOperations.Read.Name)
        {
            int? testId = resource.TestId;
            if (!testId.HasValue)
            {
                testId = resource.StudyDocument?.TestId;
            }

            if (testId.HasValue)
            {
                var hasSharedAccess = await _context.TestShares
                    .AnyAsync(ts =>
                        ts.TestId == testId.Value &&
                        ts.SharedWithUserId == userId &&
                        ts.RevokedAt == null);

                if (hasSharedAccess)
                {
                    context.Succeed(requirement);
                }
            }
        }
    }
}
