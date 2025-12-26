using Microsoft.Extensions.Logging;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;
using System.Net.Http.Json;

namespace StudieAssistenten.Client.Services;

public class ContentGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContentGenerationService> _logger;

    public ContentGenerationService(HttpClient httpClient, ILogger<ContentGenerationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GeneratedContentDto?> GenerateContentAsync(GenerateContentRequestDto request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/ContentGeneration/generate", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GeneratedContentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating {ProcessingType} content for test {TestId}",
                request.ProcessingType, request.TestId);
            throw;
        }
    }

    public async Task<List<GeneratedContentDto>> GetDocumentContentsAsync(int documentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/ContentGeneration/document/{documentId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<GeneratedContentDto>>() ?? new List<GeneratedContentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving generated contents for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<GeneratedContentDto?> GetContentAsync(int contentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/ContentGeneration/{contentId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<GeneratedContentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving generated content {ContentId}", contentId);
            throw;
        }
    }

    public async Task DeleteContentAsync(int contentId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/ContentGeneration/{contentId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting generated content {ContentId}", contentId);
            throw;
        }
    }

    public async Task<List<GeneratedContentDto>> GetTestContentsAsync(int testId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/ContentGeneration/test/{testId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<GeneratedContentDto>>() ?? new List<GeneratedContentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving generated contents for test {TestId}", testId);
            throw;
        }
    }

    public string GetPdfUrl(int contentId)
    {
        return $"api/ContentGeneration/{contentId}/pdf";
    }
}
