using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Infrastructure.Repositories;

/// <summary>
/// Repository for Test entity data access
/// </summary>
public interface ITestRepository
{
    Task<Test?> GetByIdAsync(int id, string? userId = null);
    Task<Test?> GetByIdWithDocumentsAsync(int id, string userId);
    Task<List<Test>> GetAllAsync(string userId);
    Task<List<Test>> GetAllWithDocumentsAsync(string userId);
    Task<Test> CreateAsync(Test test);
    Task UpdateAsync(Test test);
    Task<bool> DeleteAsync(int id, string userId);
    Task<bool> UserOwnsTestAsync(int testId, string userId);
}
