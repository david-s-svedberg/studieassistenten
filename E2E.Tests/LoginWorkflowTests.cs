using Microsoft.Playwright;
using FluentAssertions;

namespace E2E.Tests;

/// <summary>
/// E2E tests for the login workflow
/// </summary>
[Collection("Playwright")]
public class LoginWorkflowTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _playwrightFixture;
    private IPage? _page;
    private readonly string _baseUrl = "https://localhost:7247"; // Update this to your app URL

    public LoginWorkflowTests(PlaywrightFixture playwrightFixture)
    {
        _playwrightFixture = playwrightFixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _playwrightFixture.CreatePageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page != null)
        {
            await _page.CloseAsync();
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task LoginPage_WhenLoaded_DisplaysGoogleSignInButton()
    {
        // Arrange & Act
        await _page!.GotoAsync($"{_baseUrl}/login");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var signInButton = await _page.QuerySelectorAsync("[data-testid='google-signin-button']");
        signInButton.Should().NotBeNull("Login page should display Google sign-in button");

        var buttonText = await signInButton!.TextContentAsync();
        buttonText.Should().Contain("Sign in with Google");
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task LoginPage_WhenLoaded_DisplaysApplicationTitle()
    {
        // Arrange & Act
        await _page!.GotoAsync($"{_baseUrl}/login");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var heading = await _page.QuerySelectorAsync("h1");
        heading.Should().NotBeNull();

        var headingText = await heading!.TextContentAsync();
        headingText.Should().Be("Studieassistenten");
    }

    [Fact]
    [Trait("Category", "E2E")]
    public async Task LoginPage_WhenAccessDenied_DisplaysErrorMessage()
    {
        // Arrange & Act
        await _page!.GotoAsync($"{_baseUrl}/login?error=access_denied&email=test@example.com");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert
        var errorAlert = await _page.QuerySelectorAsync(".alert-danger");
        errorAlert.Should().NotBeNull("Error alert should be displayed");

        var errorText = await errorAlert!.TextContentAsync();
        errorText.Should().Contain("Access denied");
    }
}
