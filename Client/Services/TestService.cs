using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using StudieAssistenten.Shared.DTOs;

namespace StudieAssistenten.Client.Services;

public interface ITestService
{
    Task<TestDto?> CreateTestAsync(CreateTestRequest request);
    Task<List<TestDto>> GetAllTestsAsync();
    Task<TestDto?> GetTestAsync(int testId);
    Task<bool> UpdateTestAsync(int testId, CreateTestRequest request);
    Task<bool> DeleteTestAsync(int testId);
    Task<string?> SuggestTestNameAsync(int testId);
}

public class TestService : ITestService
{
    private readonly HttpClient _http;
    private readonly ILogger<TestService> _logger;

    public TestService(HttpClient http, ILogger<TestService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<TestDto?> CreateTestAsync(CreateTestRequest request)
    {
        try
        {
            _logger.LogInformation("Creating new test: {TestName}", request.Name);
            var response = await _http.PostAsJsonAsync("api/tests", request);
            if (response.IsSuccessStatusCode)
            {
                var test = await response.Content.ReadFromJsonAsync<TestDto>();
                _logger.LogInformation("Test created successfully: {TestId}", test?.Id);
                return test;
            }
            _logger.LogWarning("Failed to create test. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test");
            return null;
        }
    }

    public async Task<List<TestDto>> GetAllTestsAsync()
    {
        try
        {
            var tests = await _http.GetFromJsonAsync<List<TestDto>>("api/tests");
            _logger.LogInformation("Retrieved {TestCount} tests", tests?.Count ?? 0);
            return tests ?? new List<TestDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tests");
            return new List<TestDto>();
        }
    }

    public async Task<TestDto?> GetTestAsync(int testId)
    {
        try
        {
            var test = await _http.GetFromJsonAsync<TestDto>($"api/tests/{testId}");
            if (test != null)
            {
                _logger.LogInformation("Retrieved test {TestId}: {TestName}", testId, test.Name);
            }
            return test;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving test {TestId}", testId);
            return null;
        }
    }

    public async Task<bool> UpdateTestAsync(int testId, CreateTestRequest request)
    {
        try
        {
            _logger.LogInformation("Updating test {TestId}", testId);
            var response = await _http.PutAsJsonAsync($"api/tests/{testId}", request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Test {TestId} updated successfully", testId);
            }
            else
            {
                _logger.LogWarning("Failed to update test {TestId}. Status: {StatusCode}", testId, response.StatusCode);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating test {TestId}", testId);
            return false;
        }
    }

    public async Task<bool> DeleteTestAsync(int testId)
    {
        try
        {
            _logger.LogInformation("Deleting test {TestId}", testId);
            var response = await _http.DeleteAsync($"api/tests/{testId}");
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Test {TestId} deleted successfully", testId);
            }
            else
            {
                _logger.LogWarning("Failed to delete test {TestId}. Status: {StatusCode}", testId, response.StatusCode);
            }
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting test {TestId}", testId);
            return false;
        }
    }

    public async Task<string?> SuggestTestNameAsync(int testId)
    {
        try
        {
            _logger.LogInformation("Requesting test name suggestion for test {TestId}", testId);
            var response = await _http.PostAsync($"api/tests/{testId}/suggest-name", null);
            if (response.IsSuccessStatusCode)
            {
                // Read as plain text and remove any surrounding quotes
                var result = await response.Content.ReadAsStringAsync();

                // Remove JSON quotes if present
                if (result.StartsWith("\"") && result.EndsWith("\""))
                {
                    result = result.Substring(1, result.Length - 2);
                }

                _logger.LogInformation("Test name suggested for test {TestId}: {SuggestedName}", testId, result);
                return result;
            }
            _logger.LogWarning("Failed to suggest test name for test {TestId}. Status: {StatusCode}", testId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting test name for test {TestId}", testId);
            return null;
        }
    }
}
