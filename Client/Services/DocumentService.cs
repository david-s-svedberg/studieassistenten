using System.Net.Http.Json;
using StudieAssistenten.Shared.DTOs;

namespace StudieAssistenten.Client.Services;

/// <summary>
/// Service for communicating with the Documents API
/// </summary>
public interface IDocumentService
{
    Task<DocumentDto?> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, long fileSize, string? teacherInstructions = null);
    Task<List<DocumentDto>> GetAllDocumentsAsync();
    Task<DocumentDto?> GetDocumentAsync(int documentId);
    Task<bool> DeleteDocumentAsync(int documentId);
    Task<bool> TriggerOcrProcessingAsync(int documentId);
    Task<bool> UpdateExtractedTextAsync(int documentId, string text);
}

public class DocumentService : IDocumentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(HttpClient httpClient, ILogger<DocumentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<DocumentDto?> UploadDocumentAsync(
        Stream fileStream, 
        string fileName, 
        string contentType, 
        long fileSize,
        string? teacherInstructions = null)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            if (!string.IsNullOrWhiteSpace(teacherInstructions))
            {
                content.Add(new StringContent(teacherInstructions), "teacherInstructions");
            }

            var response = await _httpClient.PostAsync("api/documents/upload", content);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<DocumentDto>();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Upload failed: {StatusCode} - {Error}", response.StatusCode, error);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document: {FileName}", fileName);
            return null;
        }
    }

    public async Task<List<DocumentDto>> GetAllDocumentsAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<DocumentDto>>("api/documents") ?? new List<DocumentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents");
            return new List<DocumentDto>();
        }
    }

    public async Task<DocumentDto?> GetDocumentAsync(int documentId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DocumentDto>($"api/documents/{documentId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {DocumentId}", documentId);
            return null;
        }
    }

    public async Task<bool> DeleteDocumentAsync(int documentId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/documents/{documentId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<bool> TriggerOcrProcessingAsync(int documentId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/processing/{documentId}/extract", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering OCR for document {DocumentId}", documentId);
            return false;
        }
    }

    public async Task<bool> UpdateExtractedTextAsync(int documentId, string text)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/processing/{documentId}/text", new { Text = text });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating text for document {DocumentId}", documentId);
            return false;
        }
    }
}
