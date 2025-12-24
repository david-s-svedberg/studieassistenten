using System.Net.Http.Json;
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

    public TestService(HttpClient http)
    {
        _http = http;
    }

    public async Task<TestDto?> CreateTestAsync(CreateTestRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/tests", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TestDto>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<TestDto>> GetAllTestsAsync()
    {
        try
        {
            var tests = await _http.GetFromJsonAsync<List<TestDto>>("api/tests");
            return tests ?? new List<TestDto>();
        }
        catch
        {
            return new List<TestDto>();
        }
    }

    public async Task<TestDto?> GetTestAsync(int testId)
    {
        try
        {
            return await _http.GetFromJsonAsync<TestDto>($"api/tests/{testId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateTestAsync(int testId, CreateTestRequest request)
    {
        try
        {
            var response = await _http.PutAsJsonAsync($"api/tests/{testId}", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteTestAsync(int testId)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/tests/{testId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> SuggestTestNameAsync(int testId)
    {
        try
        {
            var response = await _http.PostAsync($"api/tests/{testId}/suggest-name", null);
            if (response.IsSuccessStatusCode)
            {
                // Read as plain text and remove any surrounding quotes
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[DEBUG] Raw response: '{result}'");

                // Remove JSON quotes if present
                if (result.StartsWith("\"") && result.EndsWith("\""))
                {
                    result = result.Substring(1, result.Length - 2);
                }

                return result;
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] SuggestTestNameAsync failed: {ex.Message}");
            return null;
        }
    }
}
