using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Infrastructure.Repositories;

/// <summary>
/// Repository for GeneratedContent entity data access
/// </summary>
public interface IGeneratedContentRepository
{
    Task<GeneratedContent?> GetByIdAsync(int id);
    Task<GeneratedContent?> GetByIdWithDetailsAsync(int id, string userId);
    Task<List<GeneratedContent>> GetByDocumentIdAsync(int documentId, string userId);
    Task<List<GeneratedContent>> GetByTestIdAsync(int testId, string userId);
    Task<List<GeneratedContent>> GetByTypeAndDocumentIdAsync(ProcessingType type, int documentId, string userId);
    Task<GeneratedContent> CreateAsync(GeneratedContent content);
    Task<bool> DeleteAsync(int id, string userId);
    Task<bool> UserOwnsContentAsync(int contentId, string userId);
}
