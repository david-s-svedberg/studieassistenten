using Microsoft.Playwright;
using System.Net.Http.Json;

namespace E2E.Tests;

/// <summary>
/// Helper class for handling authentication in E2E tests using the test-signin endpoint
/// </summary>
public static class E2EAuthHelper
{
    /// <summary>
    /// Authenticates a test user using the development-only test-signin endpoint
    /// and sets the authentication cookie in the Playwright context
    /// </summary>
    /// <param name="context">Playwright browser context</param>
    /// <param name="baseUrl">Base URL of the application</param>
    /// <param name="email">Email of the test user (default: test@example.com)</param>
    /// <param name="name">Name of the test user (default: Test User)</param>
    /// <returns>True if authentication succeeded</returns>
    public static async Task<bool> SignInAsync(
        IBrowserContext context,
        string baseUrl,
        string email = "test@example.com",
        string name = "Test User")
    {
        try
        {
            // First, navigate to the base URL to establish the domain for cookies
            var page = await context.NewPageAsync();
            await page.GotoAsync(baseUrl);

            // Call the test-signin endpoint using HTTP client
            using var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            var response = await httpClient.PostAsJsonAsync("/api/auth/test-signin", new
            {
                Email = email,
                Name = name
            });

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Test sign-in failed: {response.StatusCode}");
                return false;
            }

            // Extract the authentication cookie from the response
            if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                foreach (var cookieHeader in cookies)
                {
                    // Parse and add cookies to Playwright context
                    var cookie = ParseCookie(cookieHeader, baseUrl);
                    if (cookie != null)
                    {
                        await context.AddCookiesAsync(new[] { cookie });
                    }
                }
            }

            // Reload the page to apply the authentication cookie
            await page.ReloadAsync();
            await page.CloseAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during test sign-in: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Alternative method: Sign in by navigating to the endpoint and capturing cookies directly
    /// This method works by making the request through the browser to capture cookies automatically
    /// </summary>
    public static async Task<bool> SignInViaBrowserAsync(
        IPage page,
        string baseUrl,
        string email = "test@example.com",
        string name = "Test User")
    {
        try
        {
            // Navigate to a simple page first to establish domain
            await page.GotoAsync(baseUrl);

            // Use page.evaluate to call the API and get cookies set automatically
            var result = await page.EvaluateAsync<bool>(@"
                async (args) => {
                    try {
                        const response = await fetch('/api/auth/test-signin', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            credentials: 'include',
                            body: JSON.stringify({
                                email: args.email,
                                name: args.name
                            })
                        });
                        return response.ok;
                    } catch (e) {
                        console.error('Sign in error:', e);
                        return false;
                    }
                }
            ", new { email, name });

            // Give the browser a moment to process the cookies
            await Task.Delay(500);

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during browser-based test sign-in: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Parse a Set-Cookie header into a Playwright cookie
    /// </summary>
    private static Cookie? ParseCookie(string cookieHeader, string baseUrl)
    {
        try
        {
            var parts = cookieHeader.Split(';');
            if (parts.Length == 0) return null;

            var nameValue = parts[0].Split('=', 2);
            if (nameValue.Length != 2) return null;

            var uri = new Uri(baseUrl);
            var cookie = new Cookie
            {
                Name = nameValue[0].Trim(),
                Value = nameValue[1].Trim(),
                Domain = uri.Host,
                Path = "/",
                Secure = uri.Scheme == "https",
                HttpOnly = true,
                SameSite = SameSiteAttribute.Lax
            };

            // Parse additional attributes
            foreach (var part in parts.Skip(1))
            {
                var attr = part.Trim().ToLower();
                if (attr.StartsWith("path="))
                {
                    cookie.Path = attr.Substring(5);
                }
                else if (attr.StartsWith("domain="))
                {
                    cookie.Domain = attr.Substring(7);
                }
                else if (attr == "secure")
                {
                    cookie.Secure = true;
                }
                else if (attr == "httponly")
                {
                    cookie.HttpOnly = true;
                }
                else if (attr.StartsWith("samesite="))
                {
                    var sameSite = attr.Substring(9);
                    cookie.SameSite = sameSite switch
                    {
                        "strict" => SameSiteAttribute.Strict,
                        "lax" => SameSiteAttribute.Lax,
                        "none" => SameSiteAttribute.None,
                        _ => SameSiteAttribute.Lax
                    };
                }
            }

            return cookie;
        }
        catch
        {
            return null;
        }
    }
}
