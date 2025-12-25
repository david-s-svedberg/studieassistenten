using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using StudieAssistenten.Shared.Models;
using System.Security.Claims;

namespace StudieAssistenten.Server.Authorization;

/// <summary>
/// Authorization handler for GeneratedContent resources
/// </summary>
public class GeneratedContentAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, GeneratedContent>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        GeneratedContent resource)
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

        // User owns the content if they own the test that owns the document
        if (resource.StudyDocument != null &&
            resource.StudyDocument.Test != null &&
            resource.StudyDocument.Test.UserId == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
