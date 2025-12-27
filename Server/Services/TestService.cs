using AutoMapper;
using StudieAssistenten.Server.Infrastructure.Repositories;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services;

public interface ITestService
{
    Task<TestDto?> CreateTestAsync(CreateTestRequest request, string userId);
    Task<List<TestDto>> GetAllTestsAsync(string userId);
    Task<List<TestListDto>> GetAllTestsListAsync(string userId);
    Task<PagedResultDto<TestListDto>> GetAllTestsListPagedAsync(string userId, int pageNumber, int pageSize);
    Task<TestDto?> GetTestAsync(int testId, string? userId = null);
    Task<TestDetailDto?> GetTestDetailAsync(int testId, string? userId = null);
    Task<bool> UpdateTestAsync(int testId, CreateTestRequest request, string userId);
    Task<bool> DeleteTestAsync(int testId, string userId);
}

public class TestService : ITestService
{
    private readonly ITestRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILogger<TestService> _logger;

    public TestService(
        ITestRepository repository,
        IMapper mapper,
        ILogger<TestService> logger)
    {
        _repository = repository;
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
            UserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        test = await _repository.CreateAsync(test);

        _logger.LogInformation("Test created: {TestName} (ID: {TestId}) for User: {UserId}", test.Name, test.Id, userId);

        return _mapper.Map<TestDto>(test);
    }

    public async Task<List<TestDto>> GetAllTestsAsync(string userId)
    {
        var tests = await _repository.GetAllWithDocumentsAsync(userId);
        return _mapper.Map<List<TestDto>>(tests);
    }

    public async Task<List<TestListDto>> GetAllTestsListAsync(string userId)
    {
        var tests = await _repository.GetAllWithSharedAsync(userId);
        var testDtos = _mapper.Map<List<TestListDto>>(tests);

        // Set IsOwner flag for each test
        foreach (var dto in testDtos)
        {
            var test = tests.First(t => t.Id == dto.Id);
            dto.IsOwner = test.UserId == userId;
        }

        return testDtos;
    }

    public async Task<PagedResultDto<TestListDto>> GetAllTestsListPagedAsync(string userId, int pageNumber, int pageSize)
    {
        var allTests = await _repository.GetAllWithSharedAsync(userId);
        var totalCount = allTests.Count;

        var pagedTests = allTests
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = _mapper.Map<List<TestListDto>>(pagedTests);

        // Set IsOwner flag for each test
        foreach (var dto in items)
        {
            var test = pagedTests.First(t => t.Id == dto.Id);
            dto.IsOwner = test.UserId == userId;
        }

        return new PagedResultDto<TestListDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<TestDto?> GetTestAsync(int testId, string? userId = null)
    {
        var test = await _repository.GetByIdAsync(testId, userId);
        return test != null ? _mapper.Map<TestDto>(test) : null;
    }

    public async Task<TestDetailDto?> GetTestDetailAsync(int testId, string? userId = null)
    {
        var test = await _repository.GetByIdWithDocumentsAsync(testId, userId);
        return test != null ? _mapper.Map<TestDetailDto>(test) : null;
    }

    public async Task<bool> UpdateTestAsync(int testId, CreateTestRequest request, string userId)
    {
        var test = await _repository.GetByIdAsync(testId, userId);

        if (test == null)
        {
            return false;
        }

        test.Name = request.Name;
        test.Description = request.Description;
        test.Instructions = request.Instructions;
        test.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(test);

        _logger.LogInformation("Test updated: {TestName} (ID: {TestId})", test.Name, test.Id);

        return true;
    }

    public async Task<bool> DeleteTestAsync(int testId, string userId)
    {
        var success = await _repository.DeleteAsync(testId, userId);

        if (success)
        {
            _logger.LogInformation("Test deleted: ID {TestId}", testId);
        }

        return success;
    }
}
