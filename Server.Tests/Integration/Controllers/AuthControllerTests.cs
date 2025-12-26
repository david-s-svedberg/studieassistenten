using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Tests.Fixtures;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for AuthController.
/// Tests authentication, user info retrieval, and logout functionality.
/// </summary>
[Collection("Sequential")]
public class AuthControllerTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client;
    private ApplicationUser _testUser = null!;
    private DatabaseFixture _dbFixture = null!;

    public AuthControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Set up database fixture and test user for each test
        var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _dbFixture = new DatabaseFixture(_factory.Services);

        _testUser = await _dbFixture.CreateTestUser();

        // Create authenticated client
        _client.Dispose();
        _client = AuthenticationFixture.CreateAuthenticatedClient(_factory, _testUser);
    }

    public async Task DisposeAsync()
    {
        await _dbFixture.ClearDatabase();
        _dbFixture.Dispose();
        _client.Dispose();
    }

    #region GET /api/auth/user

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ReturnsUserInfo()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Id.Should().Be(_testUser.Id);
        userDto.Email.Should().Be(_testUser.Email);
        userDto.FullName.Should().Be(_testUser.FullName);
    }

    [Fact]
    public async Task GetCurrentUser_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange - Create an unauthenticated client
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync("/api/auth/user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Cleanup
        unauthenticatedClient.Dispose();
    }

    #endregion

    #region POST /api/auth/logout

    [Fact]
    public async Task Logout_WhenAuthenticated_ReturnsOk()
    {
        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<object>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Logout_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange - Create an unauthenticated client
        var unauthenticatedClient = _factory.CreateClient();

        // Act
        var response = await unauthenticatedClient.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Cleanup
        unauthenticatedClient.Dispose();
    }

    #endregion

    // Note: Google OAuth endpoints (login-google, google-callback) are not tested
    // in integration tests because Google authentication is disabled in the test
    // environment. OAuth flows should be tested through E2E tests or manual testing.
}
