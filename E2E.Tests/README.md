# E2E Tests for Studieassistenten

## Overview

This project contains End-to-End (E2E) tests for the Studieassistenten application using **Playwright for .NET**. These tests validate critical user workflows by interacting with the application in a real browser.

## Prerequisites

1. **.NET 8 SDK** installed
2. **Playwright browsers** installed (done automatically during project build)
3. **Running application instance** at `https://localhost:7247`

## Setup

### 1. Install Playwright Browsers

If not already installed, run:

```bash
cd E2E.Tests
dotnet build
powershell bin/Debug/net8.0/playwright.ps1 install
```

### 2. Start the Application

E2E tests require the application to be running. In a separate terminal:

```bash
cd ../Server
dotnet run
```

The application should be accessible at `https://localhost:7247`.

## Running Tests

### Run All E2E Tests

```bash
dotnet test
```

**Note:** Most tests are currently skipped by default because they require:
- A running application instance
- Configured authentication (Google OAuth)
- Test data setup

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~LoginWorkflowTests.LoginPage_WhenLoaded_DisplaysGoogleSignInButton"
```

### Un-skip Tests

To run the tests, remove the `Skip` parameter from the `[Fact]` attributes:

```csharp
// Before:
[Fact(Skip = "Requires running application - unskip when app is running")]

// After:
[Fact]
public async Task LoginPage_WhenLoaded_DisplaysGoogleSignInButton()
```

## Test Structure

```
E2E.Tests/
├── PlaywrightFixture.cs          # Browser management fixture
├── LoginWorkflowTests.cs          # Login page tests
└── README.md                      # This file
```

## Writing New E2E Tests

### Basic Pattern

```csharp
public class MyE2ETests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _playwrightFixture;
    private IPage? _page;
    private readonly string _baseUrl = "https://localhost:7247";

    public MyE2ETests(PlaywrightFixture playwrightFixture)
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
    public async Task MyTest_WhenCondition_ExpectedBehavior()
    {
        // Arrange
        await _page!.GotoAsync($"{_baseUrl}/my-page");

        // Act
        var element = await _page.QuerySelectorAsync("[data-testid='my-element']");
        await element!.ClickAsync();

        // Assert
        var result = await _page.QuerySelectorAsync("[data-testid='result']");
        result.Should().NotBeNull();
    }
}
```

### Using Test IDs

The application uses `data-testid` attributes for reliable element selection:

```csharp
// Find element by test ID
var button = await _page.QuerySelectorAsync("[data-testid='create-test-button']");

// Click it
await button!.ClickAsync();

// Wait for navigation
await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
```

### Common Playwright Operations

```csharp
// Navigate to page
await _page.GotoAsync("https://localhost:7247/tests");

// Wait for element
await _page.WaitForSelectorAsync("[data-testid='test-card']");

// Fill input
await _page.FillAsync("[data-testid='test-name-input']", "My Test");

// Click button
await _page.ClickAsync("[data-testid='save-test-button']");

// Get text content
var text = await element.TextContentAsync();

// Take screenshot
await _page.ScreenshotAsync(new() { Path = "screenshot.png" });
```

## Authentication Solution ✅

The application uses Google OAuth for authentication, which was challenging for E2E testing. We've implemented a **test authentication endpoint** that works only in Development mode.

### Test Authentication Endpoint

**Endpoint:** `POST /api/auth/test-signin`

**Security:**
- Only compiled in DEBUG builds (`#if DEBUG`)
- Double-checked at runtime (returns 404 in non-Development environments)
- **NEVER deployed to production** (excluded from Release builds)

**Usage in E2E Tests:**

```csharp
// Option 1: Using the E2EAuthHelper (recommended)
await E2EAuthHelper.SignInViaBrowserAsync(page, baseUrl, "test@example.com", "Test User");

// Option 2: Manual fetch via JavaScript
var result = await page.EvaluateAsync<bool>(@"
    async (args) => {
        const response = await fetch('/api/auth/test-signin', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ email: args.email, name: args.name })
        });
        return response.ok;
    }
", new { email = "test@example.com", name = "Test User" });
```

**How It Works:**
1. Creates or finds a test user in the database
2. Signs them in using ASP.NET Identity's `SignInManager`
3. Sets authentication cookies (same as real OAuth flow)
4. Tests can now access protected endpoints

**Test Users:**
- Default: `test@example.com` (created automatically)
- Custom: Pass any email/name to the endpoint

## CI/CD Integration

### GitHub Actions Example

```yaml
name: E2E Tests

on: [push, pull_request]

jobs:
  e2e-tests:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Install Playwright
        run: pwsh E2E.Tests/bin/Debug/net8.0/playwright.ps1 install

      - name: Start Application
        run: |
          cd Server
          dotnet run &
          sleep 10 # Wait for app to start

      - name: Run E2E Tests
        run: dotnet test E2E.Tests --no-build --verbosity normal

      - name: Upload Screenshots on Failure
        if: failure()
        uses: actions/upload-artifact@v3
        with:
          name: screenshots
          path: E2E.Tests/screenshots/
```

## Troubleshooting

### Tests Timeout
- Increase timeout in `playwright.ps1` or test code
- Ensure application is fully started before running tests
- Check network speed (Playwright downloads browser on first run)

### SSL Certificate Errors
- Tests use `IgnoreHTTPSErrors = true` in PlaywrightFixture
- Ensure development certificate is trusted: `dotnet dev-certs https --trust`

### Browser Not Found
- Run `pwsh bin/Debug/net8.0/playwright.ps1 install`
- Check that Playwright version matches in .csproj

## Best Practices

1. **Use `data-testid` attributes** for element selection (more reliable than CSS selectors)
2. **Wait for elements** before interacting (`WaitForSelectorAsync`)
3. **Use `LoadState.NetworkIdle`** to ensure page is fully loaded
4. **Take screenshots** on failure for debugging
5. **Keep tests independent** - each test should set up its own data
6. **Clean up after tests** - use `IAsyncLifetime.DisposeAsync()`

## Resources

- [Playwright for .NET Documentation](https://playwright.dev/dotnet/)
- [Playwright API Reference](https://playwright.dev/dotnet/docs/api/class-playwright)
- [Best Practices for E2E Testing](https://playwright.dev/dotnet/docs/best-practices)

## Current Status

**Phase 3: E2E Testing** - ✅ INFRASTRUCTURE COMPLETE

- ✅ Project created and configured
- ✅ Playwright installed (Chromium, Firefox, WebKit)
- ✅ PlaywrightFixture created
- ✅ `data-testid` attributes added to key UI elements
- ✅ Test authentication endpoint implemented (`/api/auth/test-signin`)
- ✅ E2EAuthHelper created for easy authentication
- ✅ Login workflow tests created (3 tests)
- ✅ Authenticated workflow tests created (6 tests):
  - Sign in and navigate to tests page
  - Create test and verify it appears in list
  - Create and view test details
  - Verify generation buttons behavior
  - Delete test from list
  - Additional UI interaction scenarios
- 📋 Pending: CI/CD integration
- 📋 Pending: File upload E2E tests (requires multipart form handling)

**Test Files:**
- `LoginWorkflowTests.cs`: 3 tests for login page (skipped by default)
- `AuthenticatedWorkflowTests.cs`: 6 tests for authenticated user workflows (skipped by default)
- `E2EAuthHelper.cs`: Authentication helper for E2E tests
- `PlaywrightFixture.cs`: Browser management

**Total E2E Tests:** 9 tests (all skipped by default - unskip when running application)

**Next Steps:**
1. Start the application (`dotnet run` in Server directory)
2. Unskip tests (remove `Skip` parameter from `[Fact]` attributes)
3. Run tests: `dotnet test`
4. Add screenshot capture on test failure
5. Configure CI/CD pipeline
