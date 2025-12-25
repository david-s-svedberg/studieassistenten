using AutoMapper;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace StudieAssistenten.Server.Services;

public interface ITestService
{
    Task<TestDto?> CreateTestAsync(CreateTestRequest request, string userId);
    Task<List<TestDto>> GetAllTestsAsync(string userId);
    Task<List<TestListDto>> GetAllTestsListAsync(string userId);
    Task<TestDto?> GetTestAsync(int testId, string? userId = null);
    Task<TestDetailDto?> GetTestDetailAsync(int testId, string userId);
    Task<bool> UpdateTestAsync(int testId, CreateTestRequest request, string userId);
    Task<bool> DeleteTestAsync(int testId, string userId);
}

public class TestService : ITestService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<TestService> _logger;

    public TestService(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<TestService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<TestDto?> CreateTestAsync(CreateTestRequest request, string userId)
    {
        var test = new Test
        {
            Name = request.Name,
            Description = request.Description,
            Instructions = request.Instructions,
            UserId = userId, // Required
            CreatedAt = DateTime.UtcNow
        };

        _context.Tests.Add(test);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Test created: {TestName} (ID: {TestId}) for User: {UserId}", test.Name, test.Id, userId);

        return _mapper.Map<TestDto>(test);
    }

    public async Task<List<TestDto>> GetAllTestsAsync(string userId)
    {
        var tests = await _context.Tests
            .Include(t => t.Documents)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<TestDto>>(tests);
    }

    public async Task<List<TestListDto>> GetAllTestsListAsync(string userId)
    {
        var tests = await _context.Tests
            .Include(t => t.Documents)
                .ThenInclude(d => d.GeneratedContents)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<TestListDto>>(tests);
    }

    public async Task<TestDto?> GetTestAsync(int testId, string? userId = null)
    {
        var query = _context.Tests
            .Include(t => t.Documents)
            .Where(t => t.Id == testId);

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(t => t.UserId == userId);
        }

        var test = await query.FirstOrDefaultAsync();

        return test != null ? _mapper.Map<TestDto>(test) : null;
    }

    public async Task<TestDetailDto?> GetTestDetailAsync(int testId, string userId)
    {
        var test = await _context.Tests
            .Include(t => t.Documents)
                .ThenInclude(d => d.GeneratedContents)
            .Where(t => t.Id == testId && t.UserId == userId)
            .FirstOrDefaultAsync();

        return test != null ? _mapper.Map<TestDetailDto>(test) : null;
    }

    public async Task<bool> UpdateTestAsync(int testId, CreateTestRequest request, string userId)
    {
        var test = await _context.Tests
            .Where(t => t.Id == testId && t.UserId == userId) // Ownership check
            .FirstOrDefaultAsync();

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

    public async Task<bool> DeleteTestAsync(int testId, string userId)
    {
        var test = await _context.Tests
            .Where(t => t.Id == testId && t.UserId == userId) // Ownership check
            .FirstOrDefaultAsync();

        if (test == null)
        {
            return false;
        }

        _context.Tests.Remove(test);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Test deleted: ID {TestId}", testId);

        return true;
    }
}
