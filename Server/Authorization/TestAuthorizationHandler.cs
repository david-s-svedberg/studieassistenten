using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using StudieAssistenten.Shared.Models;
using System.Security.Claims;

namespace StudieAssistenten.Server.Authorization;

/// <summary>
/// Authorization handler for Test resources
/// </summary>
public class TestAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Test>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Test resource)
    {
        if (context.User == null || resource == null)
        {
            return Task.CompletedTask;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask;
        }

        // User owns the test if their ID matches the test's UserId
        if (resource.UserId == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
