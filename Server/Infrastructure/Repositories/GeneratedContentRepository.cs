using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for GeneratedContent entity
/// </summary>
public class GeneratedContentRepository : IGeneratedContentRepository
{
    private readonly ApplicationDbContext _context;

    public GeneratedContentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GeneratedContent?> GetByIdAsync(int id)
    {
        return await _context.GeneratedContents.FindAsync(id);
    }

    public async Task<GeneratedContent?> GetByIdWithDetailsAsync(int id, string userId)
    {
        return await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Include(gc => gc.StudyDocument)
                .ThenInclude(d => d!.Test)
            .Where(gc => gc.Id == id && gc.StudyDocument != null && gc.StudyDocument.Test != null && gc.StudyDocument.Test.UserId == userId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<GeneratedContent>> GetByDocumentIdAsync(int documentId, string userId)
    {
        return await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Include(gc => gc.StudyDocument)
                .ThenInclude(d => d!.Test)
            .Where(gc => gc.StudyDocumentId == documentId &&
                   gc.StudyDocument != null &&
                   gc.StudyDocument.Test != null &&
                   gc.StudyDocument.Test.UserId == userId)
            .OrderByDescending(gc => gc.GeneratedAt)
            .ToListAsync();
    }

    public async Task<List<GeneratedContent>> GetByTestIdAsync(int testId, string userId)
    {
        return await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Include(gc => gc.StudyDocument)
                .ThenInclude(d => d!.Test)
            .Where(gc => gc.StudyDocument != null &&
                   gc.StudyDocument.TestId == testId &&
                   gc.StudyDocument.Test != null &&
                   gc.StudyDocument.Test.UserId == userId)
            .OrderByDescending(gc => gc.GeneratedAt)
            .ToListAsync();
    }

    public async Task<List<GeneratedContent>> GetByTypeAndDocumentIdAsync(ProcessingType type, int documentId, string userId)
    {
        return await _context.GeneratedContents
            .Include(gc => gc.Flashcards)
            .Include(gc => gc.StudyDocument)
                .ThenInclude(d => d!.Test)
            .Where(gc => gc.ProcessingType == type &&
                   gc.StudyDocumentId == documentId &&
                   gc.StudyDocument != null &&
                   gc.StudyDocument.Test != null &&
                   gc.StudyDocument.Test.UserId == userId)
            .OrderByDescending(gc => gc.GeneratedAt)
            .ToListAsync();
    }

    public async Task<GeneratedContent> CreateAsync(GeneratedContent content)
    {
        _context.GeneratedContents.Add(content);
        await _context.SaveChangesAsync();
        return content;
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var content = await GetByIdWithDetailsAsync(id, userId);
        if (content == null)
        {
            return false;
        }

        _context.GeneratedContents.Remove(content);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UserOwnsContentAsync(int contentId, string userId)
    {
        return await _context.GeneratedContents
            .Include(gc => gc.StudyDocument)
                .ThenInclude(d => d!.Test)
            .AnyAsync(gc => gc.Id == contentId &&
                      gc.StudyDocument != null &&
                      gc.StudyDocument.Test != null &&
                      gc.StudyDocument.Test.UserId == userId);
    }
}
