using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Infrastructure.Repositories;

/// <summary>
/// Repository for Test entity data access
/// </summary>
public interface ITestRepository
{
    Task<Test?> GetByIdAsync(int id, string? userId = null);
    Task<Test?> GetByIdWithDocumentsAsync(int id, string? userId = null);
    Task<List<Test>> GetAllAsync(string userId);
    Task<List<Test>> GetAllWithDocumentsAsync(string userId);
    Task<Test> CreateAsync(Test test);
    Task UpdateAsync(Test test);
    Task<bool> DeleteAsync(int id, string userId);
    Task<bool> UserOwnsTestAsync(int testId, string userId);

    /// <summary>
    /// Get all tests owned by user plus tests shared with user
    /// </summary>
    Task<List<Test>> GetAllWithSharedAsync(string userId);

    /// <summary>
    /// Get tests shared with a specific user (not owned by them)
    /// </summary>
    Task<List<Test>> GetSharedWithUserAsync(string userId);

    /// <summary>
    /// Check if user can access a test (either owns it or has it shared)
    /// </summary>
    Task<bool> UserCanAccessTestAsync(int testId, string userId);
}
