using Microsoft.AspNetCore.Components.Authorization;
using StudieAssistenten.Shared.DTOs;
using System.Net.Http.Json;
using System.Security.Claims;

namespace StudieAssistenten.Client.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomAuthStateProvider> _logger;
    private UserDto? _currentUser;

    public CustomAuthStateProvider(HttpClient httpClient, ILogger<CustomAuthStateProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        _logger.LogInformation("CustomAuthStateProvider: GetAuthenticationStateAsync called");
        
        try
        {
            _logger.LogInformation("CustomAuthStateProvider: Calling /api/auth/user");
            
            // Call the backend to get the current user
            var user = await _httpClient.GetFromJsonAsync<UserDto>("api/auth/user");

            _logger.LogInformation("CustomAuthStateProvider: Response received, user is {IsNull}", user == null ? "null" : "not null");

            if (user != null)
            {
                _currentUser = user;

                // Create claims from user data
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                if (!string.IsNullOrEmpty(user.FullName))
                {
                    claims.Add(new Claim(ClaimTypes.Name, user.FullName));
                }

                var identity = new ClaimsIdentity(claims, "serverAuth");
                var principal = new ClaimsPrincipal(identity);

                _logger.LogInformation("User authenticated: {Email}", user.Email);
                return new AuthenticationState(principal);
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // User is not authenticated - this is expected
            _logger.LogInformation("CustomAuthStateProvider: User not authenticated (401)");
        }
        catch (HttpRequestException ex)
        {
            // Other HTTP error
            _logger.LogWarning(ex, "HTTP error retrieving user: {StatusCode}", ex.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
        }

        // Return anonymous user
        _logger.LogInformation("CustomAuthStateProvider: Returning anonymous user");
        _currentUser = null;
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        return new AuthenticationState(anonymous);
    }

    public UserDto? GetCurrentUser()
    {
        return _currentUser;
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
