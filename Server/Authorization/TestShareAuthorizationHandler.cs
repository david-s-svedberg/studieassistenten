using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using StudieAssistenten.Shared.Models;
using System.Security.Claims;

namespace StudieAssistenten.Server.Authorization;

/// <summary>
/// Authorization handler for TestShare resources
/// </summary>
public class TestShareAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, TestShare>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        TestShare resource)
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

        // Only test owner can create or delete shares
        if (requirement.Name == ResourceOperations.Create.Name ||
            requirement.Name == ResourceOperations.Delete.Name)
        {
            if (resource.OwnerId == userId)
            {
                context.Succeed(requirement);
            }
        }
        // Both owner and recipient can read share info
        else if (requirement.Name == ResourceOperations.Read.Name)
        {
            if (resource.OwnerId == userId || resource.SharedWithUserId == userId)
            {
                context.Succeed(requirement);
            }
        }

        return Task.CompletedTask;
    }
}
