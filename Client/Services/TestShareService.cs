using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using StudieAssistenten.Shared.DTOs;

namespace StudieAssistenten.Client.Services;

public interface ITestShareService
{
    Task<TestShareDto?> ShareTestAsync(CreateTestShareRequest request);
    Task<List<TestShareDto>> GetSharesForTestAsync(int testId);
    Task<List<TestShareDto>> GetSharesForUserAsync();
    Task<bool> RevokeShareAsync(int shareId);
}

public class TestShareService : ITestShareService
{
    private readonly HttpClient _http;
    private readonly ILogger<TestShareService> _logger;

    public TestShareService(HttpClient http, ILogger<TestShareService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<TestShareDto?> ShareTestAsync(CreateTestShareRequest request)
    {
        try
        {
            _logger.LogInformation("Sharing test {TestId} with {Email}", request.TestId, request.SharedWithEmail);
            var response = await _http.PostAsJsonAsync("api/testshares", request);

            if (response.IsSuccessStatusCode)
            {
                var share = await response.Content.ReadFromJsonAsync<TestShareDto>();
                _logger.LogInformation("Test shared successfully: {ShareId}", share?.Id);
                return share;
            }

            _logger.LogWarning("Failed to share test. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sharing test");
            return null;
        }
    }

    public async Task<List<TestShareDto>> GetSharesForTestAsync(int testId)
    {
        try
        {
            var shares = await _http.GetFromJsonAsync<List<TestShareDto>>($"api/testshares/test/{testId}");
            _logger.LogInformation("Retrieved {ShareCount} shares for test {TestId}", shares?.Count ?? 0, testId);
            return shares ?? new List<TestShareDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shares for test {TestId}", testId);
            return new List<TestShareDto>();
        }
    }

    public async Task<List<TestShareDto>> GetSharesForUserAsync()
    {
        try
        {
            var shares = await _http.GetFromJsonAsync<List<TestShareDto>>("api/testshares/user");
            _logger.LogInformation("Retrieved {ShareCount} shares for current user", shares?.Count ?? 0);
            return shares ?? new List<TestShareDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user shares");
            return new List<TestShareDto>();
        }
    }

    public async Task<bool> RevokeShareAsync(int shareId)
    {
        try
        {
            _logger.LogInformation("Revoking share {ShareId}", shareId);
            var response = await _http.DeleteAsync($"api/testshares/{shareId}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Share {ShareId} revoked successfully", shareId);
                return true;
            }

            _logger.LogWarning("Failed to revoke share. Status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking share {ShareId}", shareId);
            return false;
        }
    }
}
