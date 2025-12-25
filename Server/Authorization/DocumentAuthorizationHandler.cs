using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using StudieAssistenten.Shared.Models;
using System.Security.Claims;

namespace StudieAssistenten.Server.Authorization;

/// <summary>
/// Authorization handler for StudyDocument resources
/// </summary>
public class DocumentAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, StudyDocument>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        StudyDocument resource)
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

        // User owns the document if they own the test it belongs to
        if (resource.Test != null && resource.Test.UserId == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
