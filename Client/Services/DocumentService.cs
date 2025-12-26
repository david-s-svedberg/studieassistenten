using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using StudieAssistenten.Shared.DTOs;

namespace StudieAssistenten.Client.Services;

/// <summary>
/// Service for communicating with the Documents API
/// </summary>
public interface IDocumentService
{
    Task<DocumentDto?> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, long fileSize, int? testId = null);
    Task<DocumentDto?> UploadDocumentAsync(IBrowserFile file, int? testId = null);
    Task<List<DocumentDto>> GetDocumentsAsync();
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
        int? testId = null)
    {
        try
        {
            _logger.LogInformation("Uploading document: {FileName} ({FileSize} bytes) to test {TestId}",
                fileName, fileSize, testId);

            using var content = new MultipartFormDataContent();

            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(streamContent, "file", fileName);

            if (testId.HasValue)
            {
                content.Add(new StringContent(testId.Value.ToString()), "testId");
            }

            var response = await _httpClient.PostAsync("api/documents/upload", content);

            if (response.IsSuccessStatusCode)
            {
                var document = await response.Content.ReadFromJsonAsync<DocumentDto>();
                _logger.LogInformation("Document uploaded successfully: {DocumentId}", document?.Id);
                return document;
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

    public async Task<DocumentDto?> UploadDocumentAsync(IBrowserFile file, int? testId = null)
    {
        const long maxFileSize = 60 * 1024 * 1024; // 60 MB
        
        try
        {
            using var stream = file.OpenReadStream(maxFileSize);
            return await UploadDocumentAsync(stream, file.Name, file.ContentType, file.Size, testId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading browser file: {FileName}", file.Name);
            return null;
        }
    }

    public async Task<List<DocumentDto>> GetDocumentsAsync()
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
