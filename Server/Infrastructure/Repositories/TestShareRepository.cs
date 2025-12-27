using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for TestShare operations
/// </summary>
public class TestShareRepository : ITestShareRepository
{
    private readonly ApplicationDbContext _context;

    public TestShareRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TestShare?> GetByIdAsync(int id)
    {
        return await _context.TestShares
            .Include(ts => ts.Test)
            .Include(ts => ts.Owner)
            .Include(ts => ts.SharedWithUser)
            .FirstOrDefaultAsync(ts => ts.Id == id);
    }

    public async Task<TestShare?> GetActiveShareAsync(int testId, string sharedWithUserId)
    {
        return await _context.TestShares
            .Include(ts => ts.Test)
            .Include(ts => ts.Owner)
            .Include(ts => ts.SharedWithUser)
            .FirstOrDefaultAsync(ts =>
                ts.TestId == testId &&
                ts.SharedWithUserId == sharedWithUserId &&
                ts.RevokedAt == null);
    }

    public async Task<List<TestShare>> GetActiveSharesForTestAsync(int testId)
    {
        return await _context.TestShares
            .Include(ts => ts.SharedWithUser)
            .Where(ts => ts.TestId == testId && ts.RevokedAt == null)
            .OrderBy(ts => ts.SharedAt)
            .ToListAsync();
    }

    public async Task<List<TestShare>> GetActiveSharesForUserAsync(string userId)
    {
        return await _context.TestShares
            .Include(ts => ts.Test)
            .Include(ts => ts.Owner)
            .Where(ts => ts.SharedWithUserId == userId && ts.RevokedAt == null)
            .OrderByDescending(ts => ts.SharedAt)
            .ToListAsync();
    }

    public async Task<List<TestShare>> GetSharedTestsWithDetailsAsync(string userId)
    {
        return await _context.TestShares
            .Include(ts => ts.Test)
                .ThenInclude(t => t!.Documents)
            .Include(ts => ts.Test)
                .ThenInclude(t => t!.GeneratedContents)
            .Include(ts => ts.Owner)
            .Where(ts => ts.SharedWithUserId == userId && ts.RevokedAt == null)
            .OrderByDescending(ts => ts.SharedAt)
            .ToListAsync();
    }

    public async Task<TestShare> CreateAsync(TestShare share)
    {
        _context.TestShares.Add(share);
        await _context.SaveChangesAsync();

        // Reload with navigation properties
        return (await GetByIdAsync(share.Id))!;
    }

    public async Task UpdateAsync(TestShare share)
    {
        _context.TestShares.Update(share);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> RevokeShareAsync(int shareId)
    {
        var share = await _context.TestShares.FindAsync(shareId);
        if (share == null)
        {
            return false;
        }

        share.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int testId, string sharedWithUserId)
    {
        return await _context.TestShares
            .AnyAsync(ts =>
                ts.TestId == testId &&
                ts.SharedWithUserId == sharedWithUserId &&
                ts.RevokedAt == null);
    }

    public async Task<bool> UserHasAccessAsync(int testId, string userId)
    {
        // Check if user is the owner
        var isOwner = await _context.Tests
            .AnyAsync(t => t.Id == testId && t.UserId == userId);

        if (isOwner)
        {
            return true;
        }

        // Check if user has an active share
        return await _context.TestShares
            .AnyAsync(ts =>
                ts.TestId == testId &&
                ts.SharedWithUserId == userId &&
                ts.RevokedAt == null);
    }
}
