using Microsoft.Playwright;

namespace E2E.Tests;

/// <summary>
/// Fixture for managing Playwright browser instances across tests
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright? Playwright { get; private set; }
    public IBrowser? Browser { get; private set; }

    public async Task InitializeAsync()
    {
        // Install Playwright and create browser instance
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true // Run in headless mode for CI/CD
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser != null)
        {
            await Browser.CloseAsync();
            await Browser.DisposeAsync();
        }

        Playwright?.Dispose();
    }

    /// <summary>
    /// Creates a new browser context with a fresh session
    /// </summary>
    public async Task<IBrowserContext> CreateContextAsync()
    {
        if (Browser == null)
            throw new InvalidOperationException("Browser not initialized");

        return await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true // Accept self-signed certs in development
        });
    }

    /// <summary>
    /// Creates a new page in a new context
    /// </summary>
    public async Task<IPage> CreatePageAsync()
    {
        var context = await CreateContextAsync();
        return await context.NewPageAsync();
    }
}
