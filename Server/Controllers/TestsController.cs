using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudieAssistenten.Server.Services;
using StudieAssistenten.Shared.DTOs;
using System.Security.Claims;

namespace StudieAssistenten.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TestsController : BaseApiController
{
    private readonly ITestService _testService;
    private readonly IAiContentGenerationService _aiService;
    private readonly ILogger<TestsController> _logger;

    public TestsController(
        ITestService testService,
        IAiContentGenerationService aiService,
        ILogger<TestsController> logger)
    {
        _testService = testService;
        _aiService = aiService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new test
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TestDto>> Create([FromBody] CreateTestRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Test name is required");
            }

            var userId = GetCurrentUserId();

            var result = await _testService.CreateTestAsync(request, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating test");
            return StatusCode(500, "An error occurred while creating the test");
        }
    }

    /// <summary>
    /// Get all tests
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TestDto>>> GetAll()
    {
        try
        {
            var userId = GetCurrentUserId();

            var tests = await _testService.GetAllTestsAsync(userId);
            return Ok(tests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tests");
            return StatusCode(500, "An error occurred while retrieving tests");
        }
    }

    /// <summary>
    /// Get a specific test
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TestDto>> GetById(int id)
    {
        try
        {
            var userId = GetCurrentUserId();

            var test = await _testService.GetTestAsync(id, userId);
            if (test == null)
            {
                return NotFound($"Test with ID {id} not found");
            }

            return Ok(test);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving test {TestId}", id);
            return StatusCode(500, "An error occurred while retrieving the test");
        }
    }

    /// <summary>
    /// Update a test
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, [FromBody] CreateTestRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Test name is required");
            }

            var userId = GetCurrentUserId();

            var success = await _testService.UpdateTestAsync(id, request, userId);
            if (!success)
            {
                return NotFound($"Test with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating test {TestId}", id);
            return StatusCode(500, "An error occurred while updating the test");
        }
    }

    /// <summary>
    /// Delete a test
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var userId = GetCurrentUserId();

            var success = await _testService.DeleteTestAsync(id, userId);
            if (!success)
            {
                return NotFound($"Test with ID {id} not found");
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting test {TestId}", id);
            return StatusCode(500, "An error occurred while deleting the test");
        }
    }

    /// <summary>
    /// Suggest a name for the test based on uploaded document content
    /// </summary>
    [HttpPost("{id}/suggest-name")]
    public async Task<ActionResult<string>> SuggestName(int id)
    {
        try
        {
            var userId = GetCurrentUserId();

            var test = await _testService.GetTestAsync(id, userId);
            if (test == null)
            {
                return NotFound($"Test with ID {id} not found");
            }

            var suggestedName = await _aiService.SuggestTestNameAsync(id);

            // Update the test with the suggested name
            var updateRequest = new CreateTestRequest
            {
                Name = suggestedName,
                Description = test.Description ?? "",
                Instructions = test.Instructions ?? ""
            };

            await _testService.UpdateTestAsync(id, updateRequest, userId);

            return Ok(suggestedName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting name for test {TestId}", id);
            return StatusCode(500, "An error occurred while suggesting the test name");
        }
    }
}
