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

E2E tests are marked with `[Trait("Category", "E2E")]` and require a running application instance.

```bash
# Start the application first (in separate terminal)
cd ../Server
dotnet run

# Then run E2E tests
cd ../E2E.Tests
dotnet test --filter "Category=E2E"
```

**Requirements:**
- Running application instance at `https://localhost:7247`
- Application configured with test authentication endpoint (Development mode)

### Run All Tests (Including Unit/Integration Tests in Other Projects)

```bash
# From solution root
dotnet test
```

**Note:** E2E tests will be skipped automatically if the application is not running, since they're in a separate filter category.

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~LoginWorkflowTests.LoginPage_WhenLoaded_DisplaysGoogleSignInButton"
```

### Test Categorization

All E2E tests are marked with a trait for easy filtering:

```csharp
[Fact]
[Trait("Category", "E2E")]
public async Task LoginPage_WhenLoaded_DisplaysGoogleSignInButton()
{
    // Test implementation
}
```

This allows:
- **Run only E2E tests:** `dotnet test --filter "Category=E2E"`
- **Exclude E2E tests:** `dotnet test --filter "Category!=E2E"`
- **CI/CD filtering:** Separate workflows can target specific test categories

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

## CI/CD Integration ✅

E2E tests are fully integrated into GitHub Actions with a dedicated workflow.

### GitHub Actions Workflow

The `.github/workflows/e2e-tests.yml` workflow:
1. Builds the application
2. Installs Playwright browsers (Chromium only for CI)
3. Starts the application in the background
4. Waits for the application to be ready
5. Runs E2E tests using trait filtering: `--filter "Category=E2E"`
6. Uploads test results and screenshots on failure

**Key Features:**
- **Trait-based filtering:** Uses `--filter "Category=E2E"` instead of modifying source files
- **Application health check:** Waits up to 60 seconds for app to be ready
- **Test appsettings:** Creates development configuration for testing
- **Screenshot capture:** Uploads screenshots on test failure for debugging
- **Test result summary:** Displays pass/fail counts in GitHub Actions summary

### Example CI/CD Snippet

```yaml
- name: Run E2E Tests
  run: |
    cd E2E.Tests
    # Run only tests marked with [Trait("Category", "E2E")]
    dotnet test --no-build --configuration Debug --verbosity normal \
      --logger "trx;LogFileName=e2e-tests.trx" \
      --filter "Category=E2E"
  env:
    ASPNETCORE_ENVIRONMENT: Development
```

**Advantages of Trait Filtering:**
- No source file modifications in CI
- Clear test categorization
- Easy to run E2E tests locally with same command
- Can exclude E2E tests from other workflows: `--filter "Category!=E2E"`

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

**Phase 3: E2E Testing** - ✅ COMPLETE
**Phase 4: CI/CD Integration** - ✅ COMPLETE

- ✅ Project created and configured
- ✅ Playwright installed (Chromium, Firefox, WebKit)
- ✅ PlaywrightFixture created
- ✅ `data-testid` attributes added to key UI elements
- ✅ Test authentication endpoint implemented (`/api/auth/test-signin`)
- ✅ E2EAuthHelper created for easy authentication
- ✅ Login workflow tests created (3 tests)
- ✅ Authenticated workflow tests created (5 tests):
  - Sign in and navigate to tests page
  - Create test and verify it appears in list
  - Create and view test details
  - Verify generation buttons behavior
  - Delete test from list
- ✅ CI/CD integration with GitHub Actions
- ✅ Trait-based test filtering (`Category=E2E`)
- ✅ Automated test result reporting
- ✅ Screenshot capture on test failure
- 📋 Pending: File upload E2E tests (requires multipart form handling)

**Test Files:**
- `LoginWorkflowTests.cs`: 3 tests for login page
- `AuthenticatedWorkflowTests.cs`: 5 tests for authenticated user workflows
- `E2EAuthHelper.cs`: Authentication helper for E2E tests
- `PlaywrightFixture.cs`: Browser management

**Total E2E Tests:** 8 tests (all marked with `[Trait("Category", "E2E")]`)

**Running Tests:**
1. Start the application: `cd Server && dotnet run`
2. Run E2E tests: `cd E2E.Tests && dotnet test --filter "Category=E2E"`
3. View results in terminal output
