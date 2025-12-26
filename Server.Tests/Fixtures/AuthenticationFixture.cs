using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Tests.Fixtures;

/// <summary>
/// Provides utilities for creating authenticated HTTP clients for testing.
/// </summary>
public class AuthenticationFixture
{
    public const string TestAuthScheme = "TestScheme";

    /// <summary>
    /// Creates an authenticated HTTP client with a test user
    /// </summary>
    public static HttpClient CreateAuthenticatedClient(
        TestWebApplicationFactory factory,
        ApplicationUser? user = null)
    {
        var client = factory.CreateClient();

        // Add test authentication header
        var userId = user?.Id ?? "test-user-id";
        var email = user?.Email ?? "test@example.com";
        var name = user?.FullName ?? "Test User";

        // Use a simple token format that our TestAuthHandler can parse
        var token = $"{userId}:{email}:{name}";
        var encodedToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthScheme, encodedToken);

        return client;
    }

    /// <summary>
    /// Creates claims for a test user
    /// </summary>
    public static ClaimsPrincipal CreateTestPrincipal(
        string userId = "test-user-id",
        string email = "test@example.com",
        string name = "Test User")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, name)
        };

        var identity = new ClaimsIdentity(claims, TestAuthScheme);
        return new ClaimsPrincipal(identity);
    }
}

/// <summary>
/// Test authentication handler that accepts any request with a test auth header.
/// Used for testing authenticated endpoints without real authentication.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check for test authentication header
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authHeader = Request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith($"{AuthenticationFixture.TestAuthScheme} "))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            // Decode the token
            var encodedToken = authHeader.Substring($"{AuthenticationFixture.TestAuthScheme} ".Length);
            var token = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedToken));
            var parts = token.Split(':');

            if (parts.Length != 3)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid token format"));
            }

            var userId = parts[0];
            var email = parts[1];
            var name = parts[2];

            var principal = AuthenticationFixture.CreateTestPrincipal(userId, email, name);
            var ticket = new AuthenticationTicket(principal, AuthenticationFixture.TestAuthScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid token"));
        }
    }
}
