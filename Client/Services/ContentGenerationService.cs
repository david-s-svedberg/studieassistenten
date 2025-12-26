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
            _logger.LogInformation("Generating {ProcessingType} content for test {TestId}",
                request.ProcessingType, request.TestId);
            var response = await _httpClient.PostAsJsonAsync("api/ContentGeneration/generate", request);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadFromJsonAsync<GeneratedContentDto>();
            _logger.LogInformation("Content generated successfully: {ContentId}", content?.Id);
            return content;
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
            _logger.LogInformation("Retrieving generated contents for document {DocumentId}", documentId);
            var response = await _httpClient.GetAsync($"api/ContentGeneration/document/{documentId}");
            response.EnsureSuccessStatusCode();
            var contents = await response.Content.ReadFromJsonAsync<List<GeneratedContentDto>>() ?? new List<GeneratedContentDto>();
            _logger.LogInformation("Retrieved {Count} generated contents for document {DocumentId}",
                contents.Count, documentId);
            return contents;
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
            _logger.LogInformation("Retrieving generated content {ContentId}", contentId);
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
            _logger.LogInformation("Deleting generated content {ContentId}", contentId);
            var response = await _httpClient.DeleteAsync($"api/ContentGeneration/{contentId}");
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Generated content {ContentId} deleted successfully", contentId);
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
            _logger.LogInformation("Retrieving generated contents for test {TestId}", testId);
            var response = await _httpClient.GetAsync($"api/ContentGeneration/test/{testId}");
            response.EnsureSuccessStatusCode();
            var contents = await response.Content.ReadFromJsonAsync<List<GeneratedContentDto>>() ?? new List<GeneratedContentDto>();
            _logger.LogInformation("Retrieved {Count} generated contents for test {TestId}",
                contents.Count, testId);
            return contents;
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
