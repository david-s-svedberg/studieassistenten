using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Models;
using System.Security.Claims;

namespace StudieAssistenten.Server.Authorization;

/// <summary>
/// Authorization handler for Test resources
/// Checks both ownership and shared access
/// </summary>
public class TestAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Test>
{
    private readonly ApplicationDbContext _context;

    public TestAuthorizationHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Test resource)
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
        if (resource.UserId == userId)
        {
            context.Succeed(requirement);
            return;
        }

        // Check shared access - only for Read operations
        if (requirement.Name == ResourceOperations.Read.Name)
        {
            var hasSharedAccess = await _context.TestShares
                .AnyAsync(ts =>
                    ts.TestId == resource.Id &&
                    ts.SharedWithUserId == userId &&
                    ts.RevokedAt == null);

            if (hasSharedAccess)
            {
                context.Succeed(requirement);
            }
        }
    }
}
