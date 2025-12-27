using AutoMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudieAssistenten.Server.Services;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Models;
using System.Security.Claims;

namespace StudieAssistenten.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailWhitelistService _whitelistService;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailWhitelistService whitelistService,
        IMapper mapper,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _whitelistService = whitelistService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Initiate Google OAuth login
    /// </summary>
    [HttpGet("login-google")]
    public IActionResult LoginGoogle(string returnUrl = "/")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl }),
            Items = { { "scheme", GoogleDefaults.AuthenticationScheme } }
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Google OAuth callback - validates email whitelist and creates/signs in user
    /// </summary>
    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback(string returnUrl = "/")
    {
        try
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
            {
                _logger.LogWarning("Google authentication failed");
                return Redirect($"/login?error=auth_failed");
            }

            var claims = authenticateResult.Principal?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var picture = claims?.FirstOrDefault(c => c.Type == "picture")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("No email claim found in Google response");
                return Redirect($"/login?error=no_email");
            }

            // WHITELIST CHECK
            if (!_whitelistService.IsEmailWhitelisted(email))
            {
                _logger.LogWarning("Access denied for non-whitelisted email: {Email}", email);
                return Redirect($"/login?error=access_denied&email={Uri.EscapeDataString(email)}");
            }

            // Find or create user
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true, // Google verified
                    FullName = name,
                    ProfilePictureUrl = picture,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    _logger.LogError("Failed to create user: {Errors}",
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return Redirect($"/login?error=user_creation_failed");
                }

                _logger.LogInformation("Created new user: {Email}", email);
            }
            else
            {
                // Update user profile from Google
                user.FullName = name;
                user.ProfilePictureUrl = picture;
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            // Sign in the user
            await _signInManager.SignInAsync(user, isPersistent: true);

            _logger.LogInformation("User logged in: {Email}", email);

            return Redirect(returnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Google callback");
            return Redirect($"/login?error=callback_failed");
        }
    }

    /// <summary>
    /// Get current user information
    /// NOTE: This endpoint does NOT have [Authorize] to avoid redirect loops in Blazor WASM
    /// </summary>
    [HttpGet("user")]
    public async Task<IActionResult> GetCurrentUser()
    {
        // Check if user is authenticated
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return Unauthorized();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(_mapper.Map<UserDto>(user));
    }

    /// <summary>
    /// Logout the current user
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        // Sign out using ASP.NET Identity
        await _signInManager.SignOutAsync();

        // Explicitly delete the authentication cookie
        Response.Cookies.Delete("StudieAssistenten.Auth");

        // Also delete the .AspNetCore.Identity.Application cookie if it exists
        Response.Cookies.Delete(".AspNetCore.Identity.Application");

        _logger.LogInformation("User logged out: {Email}", userEmail);
        return Ok(new { message = "Logged out successfully" });
    }

#if DEBUG
    /// <summary>
    /// TEST ONLY: Sign in as a test user without OAuth (DEVELOPMENT MODE ONLY)
    /// This endpoint is compiled only in DEBUG mode and should NEVER be deployed to production.
    /// Used for E2E testing to bypass Google OAuth authentication.
    /// </summary>
    [HttpPost("test-signin")]
    public async Task<IActionResult> TestSignIn([FromBody] TestSignInRequest? request)
    {
        // Extra safety check - verify we're in Development environment
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment != "Development")
        {
            _logger.LogWarning("Attempted to use test-signin endpoint in non-Development environment: {Environment}", environment);
            return NotFound(); // Return 404 instead of revealing endpoint exists
        }

        // Default test user email
        var email = request?.Email ?? "test@example.com";
        var name = request?.Name ?? "Test User";

        _logger.LogInformation("TEST-SIGNIN: Signing in test user: {Email}", email);

        // Find or create test user
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = name,
                ProfilePictureUrl = "https://via.placeholder.com/150",
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user);

            if (!createResult.Succeeded)
            {
                _logger.LogError("TEST-SIGNIN: Failed to create test user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return BadRequest(new { error = "Failed to create test user" });
            }

            _logger.LogInformation("TEST-SIGNIN: Created new test user: {Email}", email);
        }
        else
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }

        // Sign in the test user
        await _signInManager.SignInAsync(user, isPersistent: true);

        _logger.LogInformation("TEST-SIGNIN: Test user signed in: {Email}", email);

        return Ok(new
        {
            message = "Test sign-in successful",
            userId = user.Id,
            email = user.Email,
            name = user.FullName
        });
    }
#endif
}

#if DEBUG
/// <summary>
/// Request model for test sign-in endpoint (DEBUG only)
/// </summary>
public class TestSignInRequest
{
    public string? Email { get; set; }
    public string? Name { get; set; }
}
#endif
