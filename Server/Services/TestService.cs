using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace StudieAssistenten.Server.Services;

public interface ITestService
{
    Task<TestDto?> CreateTestAsync(CreateTestRequest request, string? userId = null);
    Task<List<TestDto>> GetAllTestsAsync(string? userId = null);
    Task<TestDto?> GetTestAsync(int testId);
    Task<bool> UpdateTestAsync(int testId, CreateTestRequest request);
    Task<bool> DeleteTestAsync(int testId);
}

public class TestService : ITestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TestService> _logger;

    public TestService(ApplicationDbContext context, ILogger<TestService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TestDto?> CreateTestAsync(CreateTestRequest request, string? userId = null)
    {
        var test = new Test
        {
            Name = request.Name,
            Description = request.Description,
            Instructions = request.Instructions,
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tests.Add(test);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Test created: {TestName} (ID: {TestId})", test.Name, test.Id);

        return MapToDto(test);
    }

    public async Task<List<TestDto>> GetAllTestsAsync(string? userId = null)
    {
        var query = _context.Tests
            .Include(t => t.Documents)
            .Include(t => t.GeneratedContents)
            .AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(t => t.UserId == userId);
        }

        var tests = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tests.Select(MapToDto).ToList();
    }

    public async Task<TestDto?> GetTestAsync(int testId)
    {
        var test = await _context.Tests
            .Include(t => t.Documents)
            .Include(t => t.GeneratedContents)
            .FirstOrDefaultAsync(t => t.Id == testId);

        return test != null ? MapToDto(test) : null;
    }

    public async Task<bool> UpdateTestAsync(int testId, CreateTestRequest request)
    {
        var test = await _context.Tests.FindAsync(testId);
        if (test == null)
        {
            return false;
        }

        test.Name = request.Name;
        test.Description = request.Description;
        test.Instructions = request.Instructions;
        test.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Test updated: {TestName} (ID: {TestId})", test.Name, test.Id);

        return true;
    }

    public async Task<bool> DeleteTestAsync(int testId)
    {
        var test = await _context.Tests.FindAsync(testId);
        if (test == null)
        {
            return false;
        }

        _context.Tests.Remove(test);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Test deleted: ID {TestId}", testId);

        return true;
    }

    private static TestDto MapToDto(Test test)
    {
        return new TestDto
        {
            Id = test.Id,
            Name = test.Name,
            Description = test.Description,
            Instructions = test.Instructions,
            UserId = test.UserId,
            CreatedAt = test.CreatedAt,
            UpdatedAt = test.UpdatedAt,
            DocumentCount = test.Documents?.Count ?? 0,
            TotalCharacters = test.Documents?.Sum(d => d.ExtractedText?.Length ?? 0) ?? 0,
            HasGeneratedContent = test.GeneratedContents?.Any() ?? false,
            Documents = test.Documents?.Select(d => new DocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                FileSizeBytes = d.FileSizeBytes,
                ExtractedText = d.ExtractedText ?? string.Empty,
                Status = d.Status,
                UploadedAt = d.UploadedAt,
                TestId = d.TestId
            }).ToList() ?? new List<DocumentDto>()
        };
    }
}
