using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudieAssistenten.Server.Authorization;
using StudieAssistenten.Server.Infrastructure.Repositories;
using StudieAssistenten.Server.Services;
using StudieAssistenten.Shared.DTOs;

namespace StudieAssistenten.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TestSharesController : BaseApiController
{
    private readonly ITestShareService _shareService;
    private readonly ITestShareRepository _shareRepository;
    private readonly ITestRepository _testRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<TestSharesController> _logger;

    public TestSharesController(
        ITestShareService shareService,
        ITestShareRepository shareRepository,
        ITestRepository testRepository,
        IAuthorizationService authorizationService,
        ILogger<TestSharesController> logger)
    {
        _shareService = shareService;
        _shareRepository = shareRepository;
        _testRepository = testRepository;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Share a test with another user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TestShareDto>> Create([FromBody] CreateTestShareRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SharedWithEmail))
            {
                return BadRequest("Email is required");
            }

            // Fetch the test to check authorization
            var test = await _testRepository.GetByIdAsync(request.TestId);
            if (test == null)
            {
                return NotFound($"Test with ID {request.TestId} not found");
            }

            var userId = GetCurrentUserId();

            // Only the test owner can share it
            if (test.UserId != userId)
            {
                return Forbid();
            }

            var result = await _shareService.ShareTestAsync(request, userId);

            if (result == null)
            {
                return BadRequest("Failed to create share. User may not exist or share may already exist.");
            }

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test share");
            return StatusCode(500, "An error occurred while creating the test share");
        }
    }

    /// <summary>
    /// Get all shares for a specific test (owner only)
    /// </summary>
    [HttpGet("test/{testId}")]
    public async Task<ActionResult<List<TestShareDto>>> GetSharesForTest(int testId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var shares = await _shareService.GetSharesForTestAsync(testId, userId);
            return Ok(shares);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shares for test {TestId}", testId);
            return StatusCode(500, "An error occurred while retrieving test shares");
        }
    }

    /// <summary>
    /// Get all tests shared with the current user
    /// </summary>
    [HttpGet("user")]
    public async Task<ActionResult<List<TestShareDto>>> GetSharesForUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            var shares = await _shareService.GetSharesForUserAsync(userId);
            return Ok(shares);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shares for user");
            return StatusCode(500, "An error occurred while retrieving your shares");
        }
    }

    /// <summary>
    /// Get a specific share by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TestShareDto>> GetById(int id)
    {
        try
        {
            // Fetch the share first
            var share = await _shareRepository.GetByIdAsync(id);
            if (share == null)
            {
                return NotFound($"Share with ID {id} not found");
            }

            // Check authorization against the resource
            var authResult = await _authorizationService.AuthorizeAsync(
                User, share, ResourceOperations.Read);

            if (!authResult.Succeeded)
            {
                return Forbid();
            }

            var userId = GetCurrentUserId();
            var shareDto = await _shareService.GetShareAsync(id, userId);

            return Ok(shareDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving share {ShareId}", id);
            return StatusCode(500, "An error occurred while retrieving the share");
        }
    }

    /// <summary>
    /// Revoke a test share (owner only)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            // Fetch the share first
            var share = await _shareRepository.GetByIdAsync(id);
            if (share == null)
            {
                return NotFound($"Share with ID {id} not found");
            }

            // Check authorization against the resource
            var authResult = await _authorizationService.AuthorizeAsync(
                User, share, ResourceOperations.Delete);

            if (!authResult.Succeeded)
            {
                return Forbid();
            }

            var userId = GetCurrentUserId();
            var success = await _shareService.RevokeShareAsync(id, userId);

            if (!success)
            {
                return NotFound($"Share with ID {id} not found or you don't have permission to revoke it");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking share {ShareId}", id);
            return StatusCode(500, "An error occurred while revoking the share");
        }
    }
}
