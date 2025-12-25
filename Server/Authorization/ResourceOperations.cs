using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace StudieAssistenten.Server.Authorization;

/// <summary>
/// Defines standard authorization operations for resources
/// </summary>
public static class ResourceOperations
{
    public static OperationAuthorizationRequirement Create =
        new() { Name = nameof(Create) };

    public static OperationAuthorizationRequirement Read =
        new() { Name = nameof(Read) };

    public static OperationAuthorizationRequirement Update =
        new() { Name = nameof(Update) };

    public static OperationAuthorizationRequirement Delete =
        new() { Name = nameof(Delete) };
}
