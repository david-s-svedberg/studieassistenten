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
public class TestsController : BaseApiController
{
    private readonly ITestService _testService;
    private readonly ITestRepository _testRepository;
    private readonly IAiContentGenerationService _aiService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<TestsController> _logger;

    public TestsController(
        ITestService testService,
        ITestRepository testRepository,
        IAiContentGenerationService aiService,
        IAuthorizationService authorizationService,
        ILogger<TestsController> logger)
    {
        _testService = testService;
        _testRepository = testRepository;
        _aiService = aiService;
        _authorizationService = authorizationService;
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
    /// Get all tests (lightweight list view)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TestListDto>>> GetAll()
    {
        try
        {
            var userId = GetCurrentUserId();

            var tests = await _testService.GetAllTestsListAsync(userId);
            return Ok(tests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tests");
            return StatusCode(500, "An error occurred while retrieving tests");
        }
    }

    /// <summary>
    /// Get all tests with pagination (lightweight list view)
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page (1-100)</param>
    /// <returns>Paginated list of tests</returns>
    /// <response code="200">Returns paginated test list</response>
    /// <response code="400">Invalid pagination parameters</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResultDto<TestListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResultDto<TestListDto>>> GetAllPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            // Validate pagination parameters
            if (pageNumber < 1)
            {
                return BadRequestError("Page number must be at least 1");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequestError("Page size must be between 1 and 100");
            }

            var userId = GetCurrentUserId();

            var result = await _testService.GetAllTestsListPagedAsync(userId, pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged tests");
            return InternalServerError("An error occurred while retrieving tests", errorCode: "TESTS_RETRIEVAL_ERROR");
        }
    }

    /// <summary>
    /// Get a specific test (detailed view with documents)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TestDetailDto>> GetById(int id)
    {
        try
        {
            var userId = GetCurrentUserId();

            var test = await _testService.GetTestDetailAsync(id, userId);
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
    /// Delete a test (with resource-based authorization)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            // Fetch the resource first
            var test = await _testRepository.GetByIdAsync(id);
            if (test == null)
            {
                return NotFound($"Test with ID {id} not found");
            }

            // Check authorization against the resource
            var authResult = await _authorizationService.AuthorizeAsync(
                User, test, ResourceOperations.Delete);

            if (!authResult.Succeeded)
            {
                return Forbid();
            }

            // Proceed with deletion
            var userId = GetCurrentUserId();
            var success = await _testService.DeleteTestAsync(id, userId);

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
