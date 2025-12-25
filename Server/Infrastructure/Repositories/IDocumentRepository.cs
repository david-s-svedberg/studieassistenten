using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Infrastructure.Repositories;

/// <summary>
/// Repository for StudyDocument entity data access
/// </summary>
public interface IDocumentRepository
{
    Task<StudyDocument?> GetByIdAsync(int id);
    Task<StudyDocument?> GetByIdWithTestAsync(int id, string userId);
    Task<List<StudyDocument>> GetByTestIdAsync(int testId, string userId);
    Task<List<StudyDocument>> GetAllAsync(string userId);
    Task<StudyDocument> CreateAsync(StudyDocument document);
    Task UpdateAsync(StudyDocument document);
    Task<bool> DeleteAsync(int id, string userId);
    Task<bool> UserOwnsDocumentAsync(int documentId, string userId);
}
