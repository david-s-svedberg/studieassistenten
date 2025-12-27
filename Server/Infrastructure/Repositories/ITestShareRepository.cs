using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Infrastructure.Repositories;

/// <summary>
/// Repository interface for TestShare operations
/// </summary>
public interface ITestShareRepository
{
    /// <summary>
    /// Get a share by ID
    /// </summary>
    Task<TestShare?> GetByIdAsync(int id);

    /// <summary>
    /// Get an active share for a specific test and user combination
    /// </summary>
    Task<TestShare?> GetActiveShareAsync(int testId, string sharedWithUserId);

    /// <summary>
    /// Get all active shares for a specific test (who it's shared with)
    /// </summary>
    Task<List<TestShare>> GetActiveSharesForTestAsync(int testId);

    /// <summary>
    /// Get all active shares for a specific user (tests shared with them)
    /// </summary>
    Task<List<TestShare>> GetActiveSharesForUserAsync(string userId);

    /// <summary>
    /// Get all tests that have been shared with a user (including test details)
    /// </summary>
    Task<List<TestShare>> GetSharedTestsWithDetailsAsync(string userId);

    /// <summary>
    /// Create a new share
    /// </summary>
    Task<TestShare> CreateAsync(TestShare share);

    /// <summary>
    /// Update an existing share (e.g., revoke)
    /// </summary>
    Task UpdateAsync(TestShare share);

    /// <summary>
    /// Revoke a share by setting RevokedAt timestamp
    /// </summary>
    Task<bool> RevokeShareAsync(int shareId);

    /// <summary>
    /// Check if an active share exists for a test and user combination
    /// </summary>
    Task<bool> ExistsAsync(int testId, string sharedWithUserId);

    /// <summary>
    /// Check if a user has access to a test (either as owner or via share)
    /// </summary>
    Task<bool> UserHasAccessAsync(int testId, string userId);
}
