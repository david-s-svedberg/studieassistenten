using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for StudyDocument entity
/// </summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StudyDocument?> GetByIdAsync(int id)
    {
        return await _context.StudyDocuments.FindAsync(id);
    }

    public async Task<StudyDocument?> GetByIdWithTestAsync(int id, string userId)
    {
        return await _context.StudyDocuments
            .Include(d => d.Test)
            .Where(d => d.Id == id && d.Test != null && d.Test.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<StudyDocument>> GetByTestIdAsync(int testId, string userId)
    {
        return await _context.StudyDocuments
            .Include(d => d.Test)
            .Where(d => d.TestId == testId && d.Test != null && d.Test.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<List<StudyDocument>> GetAllAsync(string userId)
    {
        return await _context.StudyDocuments
            .Include(d => d.Test)
            .Where(d => d.Test != null && d.Test.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();
    }

    public async Task<StudyDocument> CreateAsync(StudyDocument document)
    {
        _context.StudyDocuments.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task UpdateAsync(StudyDocument document)
    {
        _context.StudyDocuments.Update(document);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var document = await GetByIdWithTestAsync(id, userId);
        if (document == null)
        {
            return false;
        }

        _context.StudyDocuments.Remove(document);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UserOwnsDocumentAsync(int documentId, string userId)
    {
        return await _context.StudyDocuments
            .Include(d => d.Test)
            .AnyAsync(d => d.Id == documentId && d.Test != null && d.Test.UserId == userId);
    }
}
