using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Test entity
/// </summary>
public class TestRepository : ITestRepository
{
    private readonly ApplicationDbContext _context;

    public TestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Test?> GetByIdAsync(int id, string? userId = null)
    {
        var query = _context.Tests.Where(t => t.Id == id);

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(t => t.UserId == userId);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<Test?> GetByIdWithDocumentsAsync(int id, string userId)
    {
        return await _context.Tests
            .Include(t => t.Documents)
                .ThenInclude(d => d.GeneratedContents)
            .Where(t => t.Id == id && t.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Test>> GetAllAsync(string userId)
    {
        return await _context.Tests
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Test>> GetAllWithDocumentsAsync(string userId)
    {
        return await _context.Tests
            .Include(t => t.Documents)
                .ThenInclude(d => d.GeneratedContents)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Test> CreateAsync(Test test)
    {
        _context.Tests.Add(test);
        await _context.SaveChangesAsync();
        return test;
    }

    public async Task UpdateAsync(Test test)
    {
        _context.Tests.Update(test);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var test = await GetByIdAsync(id, userId);
        if (test == null)
        {
            return false;
        }

        _context.Tests.Remove(test);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UserOwnsTestAsync(int testId, string userId)
    {
        return await _context.Tests
            .AnyAsync(t => t.Id == testId && t.UserId == userId);
    }
}
