using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;
using System.Net.Http.Json;

namespace StudieAssistenten.Client.Services;

public class ContentGenerationService
{
    private readonly HttpClient _httpClient;

    public ContentGenerationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GeneratedContentDto?> GenerateContentAsync(GenerateContentRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/ContentGeneration/generate", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GeneratedContentDto>();
    }

    public async Task<List<GeneratedContentDto>> GetDocumentContentsAsync(int documentId)
    {
        var response = await _httpClient.GetAsync($"api/ContentGeneration/document/{documentId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<GeneratedContentDto>>() ?? new List<GeneratedContentDto>();
    }

    public async Task<GeneratedContentDto?> GetContentAsync(int contentId)
    {
        var response = await _httpClient.GetAsync($"api/ContentGeneration/{contentId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GeneratedContentDto>();
    }

    public async Task DeleteContentAsync(int contentId)
    {
        var response = await _httpClient.DeleteAsync($"api/ContentGeneration/{contentId}");
        response.EnsureSuccessStatusCode();
    }
}
