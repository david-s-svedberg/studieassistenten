using AutoMapper;
using Microsoft.AspNetCore.Identity;
using StudieAssistenten.Server.Infrastructure.Repositories;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services;

public interface ITestShareService
{
    Task<TestShareDto?> ShareTestAsync(CreateTestShareRequest request, string ownerId);
    Task<List<TestShareDto>> GetSharesForTestAsync(int testId, string userId);
    Task<List<TestShareDto>> GetSharesForUserAsync(string userId);
    Task<bool> RevokeShareAsync(int shareId, string userId);
    Task<TestShareDto?> GetShareAsync(int shareId, string userId);
}

public class TestShareService : ITestShareService
{
    private readonly ITestShareRepository _shareRepository;
    private readonly ITestRepository _testRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<TestShareService> _logger;

    public TestShareService(
        ITestShareRepository shareRepository,
        ITestRepository testRepository,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        ILogger<TestShareService> logger)
    {
        _shareRepository = shareRepository;
        _testRepository = testRepository;
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<TestShareDto?> ShareTestAsync(CreateTestShareRequest request, string ownerId)
    {
        // Verify test exists and user owns it
        var test = await _testRepository.GetByIdAsync(request.TestId, ownerId);
        if (test == null)
        {
            _logger.LogWarning("User {UserId} attempted to share non-existent or unauthorized test {TestId}",
                ownerId, request.TestId);
            return null;
        }

        // Find user by email
        var sharedWithUser = await _userManager.FindByEmailAsync(request.SharedWithEmail);
        if (sharedWithUser == null)
        {
            _logger.LogWarning("User with email {Email} not found", request.SharedWithEmail);
            return null;
        }

        // Don't allow sharing with yourself
        if (sharedWithUser.Id == ownerId)
        {
            _logger.LogWarning("User {UserId} attempted to share test with themselves", ownerId);
            return null;
        }

        // Check if share already exists
        var existingShare = await _shareRepository.GetActiveShareAsync(request.TestId, sharedWithUser.Id);
        if (existingShare != null)
        {
            _logger.LogWarning("Active share already exists for Test {TestId} and User {UserId}",
                request.TestId, sharedWithUser.Id);
            return _mapper.Map<TestShareDto>(existingShare);
        }

        var share = new TestShare
        {
            TestId = request.TestId,
            OwnerId = ownerId,
            SharedWithUserId = sharedWithUser.Id,
            SharedAt = DateTime.UtcNow,
            Permission = SharePermission.Read
        };

        share = await _shareRepository.CreateAsync(share);

        _logger.LogInformation("Test {TestId} shared with user {SharedWithUserId} by owner {OwnerId}",
            request.TestId, sharedWithUser.Id, ownerId);

        // Load navigation properties for DTO mapping
        share.Test = test;
        share.Owner = await _userManager.FindByIdAsync(ownerId);
        share.SharedWithUser = sharedWithUser;

        return _mapper.Map<TestShareDto>(share);
    }

    public async Task<List<TestShareDto>> GetSharesForTestAsync(int testId, string userId)
    {
        // Verify user owns the test
        var ownsTest = await _testRepository.UserOwnsTestAsync(testId, userId);
        if (!ownsTest)
        {
            _logger.LogWarning("User {UserId} attempted to view shares for unauthorized test {TestId}",
                userId, testId);
            return new List<TestShareDto>();
        }

        var shares = await _shareRepository.GetActiveSharesForTestAsync(testId);
        return _mapper.Map<List<TestShareDto>>(shares);
    }

    public async Task<List<TestShareDto>> GetSharesForUserAsync(string userId)
    {
        var shares = await _shareRepository.GetActiveSharesForUserAsync(userId);
        return _mapper.Map<List<TestShareDto>>(shares);
    }

    public async Task<bool> RevokeShareAsync(int shareId, string userId)
    {
        var share = await _shareRepository.GetByIdAsync(shareId);
        if (share == null)
        {
            return false;
        }

        // Only owner can revoke shares
        if (share.OwnerId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to revoke share {ShareId} they don't own",
                userId, shareId);
            return false;
        }

        var success = await _shareRepository.RevokeShareAsync(shareId);

        if (success)
        {
            _logger.LogInformation("Share {ShareId} revoked by owner {OwnerId}", shareId, userId);
        }

        return success;
    }

    public async Task<TestShareDto?> GetShareAsync(int shareId, string userId)
    {
        var share = await _shareRepository.GetByIdAsync(shareId);
        if (share == null)
        {
            return null;
        }

        // User must be either owner or recipient
        if (share.OwnerId != userId && share.SharedWithUserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to view unauthorized share {ShareId}",
                userId, shareId);
            return null;
        }

        return _mapper.Map<TestShareDto>(share);
    }
}
