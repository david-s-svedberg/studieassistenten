using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Tests.Fixtures;
using StudieAssistenten.Server.Tests.TestData;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for TestsController.
/// Tests CRUD operations for tests with real database and mocked external services.
/// </summary>
[Collection("Sequential")]
public class TestsControllerTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client;
    private ApplicationUser _testUser = null!;
    private DatabaseFixture _dbFixture = null!;

    public TestsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Set up database fixture and test user for each test
        var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _dbFixture = new DatabaseFixture(_factory.Services);

        _testUser = await _dbFixture.CreateTestUser();

        // Create authenticated client
        _client.Dispose();
        _client = AuthenticationFixture.CreateAuthenticatedClient(_factory, _testUser);
    }

    public async Task DisposeAsync()
    {
        await _dbFixture.ClearDatabase();
        _dbFixture.Dispose();
        _client.Dispose();
    }

    #region GET /api/tests

    [Fact]
    public async Task GetAllTests_WhenNoTests_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/tests");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tests = await response.Content.ReadFromJsonAsync<List<TestDto>>();
        tests.Should().NotBeNull();
        tests.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTests_WithMultipleTests_ReturnsAllUserTests()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        await builder.Test(_testUser.Id).WithName("Test 1").BuildAsync();
        await builder.Test(_testUser.Id).WithName("Test 2").BuildAsync();
        await builder.Test(_testUser.Id).WithName("Test 3").BuildAsync();

        // Create a test for a different user (should not be returned)
        var otherUser = await _dbFixture.CreateTestUser("other@example.com", "Other User");
        await builder.Test(otherUser.Id).WithName("Other User Test").BuildAsync();

        // Act
        var response = await _client.GetAsync("/api/tests");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tests = await response.Content.ReadFromJsonAsync<List<TestDto>>();
        tests.Should().NotBeNull();
        tests.Should().HaveCount(3);
        tests.Should().OnlyContain(t => t.Name.StartsWith("Test "));
    }

    #endregion

    #region GET /api/tests/{id}

    [Fact]
    public async Task GetTest_WithValidId_ReturnsTest()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("Integration Test")
            .WithDescription("Test description")
            .WithDocument("document1.pdf")
            .WithDocument("document2.pdf")
            .BuildAsync();

        // Act
        var response = await _client.GetAsync($"/api/tests/{test.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var testDto = await response.Content.ReadFromJsonAsync<TestDto>();
        testDto.Should().NotBeNull();
        testDto!.Id.Should().Be(test.Id);
        testDto.Name.Should().Be("Integration Test");
        testDto.Description.Should().Be("Test description");
        testDto.DocumentCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTest_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/tests/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTest_WithOtherUsersTest_ReturnsForbidden()
    {
        // Arrange
        var otherUser = await _dbFixture.CreateTestUser("other@example.com", "Other User");
        var builder = new TestDataBuilder(_dbFixture.Context);
        var otherTest = await builder.Test(otherUser.Id)
            .WithName("Other User Test")
            .BuildAsync();

        // Act
        var response = await _client.GetAsync($"/api/tests/{otherTest.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/tests

    [Fact]
    public async Task CreateTest_WithValidData_CreatesTest()
    {
        // Arrange
        var request = TestDataBuilder.CreateTestRequest(
            name: "New Test",
            description: "Test description",
            instructions: "Test instructions"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/tests", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var test = await response.Content.ReadFromJsonAsync<TestDto>();
        test.Should().NotBeNull();
        test!.Name.Should().Be("New Test");
        test.Description.Should().Be("Test description");
        test.Instructions.Should().Be("Test instructions");
        test.DocumentCount.Should().Be(0);

        // Verify in database
        var dbTest = await _dbFixture.Context.Tests.FindAsync(test.Id);
        dbTest.Should().NotBeNull();
        dbTest!.UserId.Should().Be(_testUser.Id);
    }

    [Fact]
    public async Task CreateTest_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateTestRequest(name: "");

        // Act
        var response = await _client.PostAsJsonAsync("/api/tests", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTest_WithTimestampName_CreatesTestWithCurrentTimestamp()
    {
        // Arrange
        var request = TestDataBuilder.CreateTestRequest(
            name: $"Test - {DateTime.Now:yyyy-MM-dd HH:mm}"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/tests", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var test = await response.Content.ReadFromJsonAsync<TestDto>();
        test.Should().NotBeNull();
        test!.Name.Should().Contain("Test -");
        test.Name.Should().Contain(DateTime.Now.Year.ToString());
    }

    #endregion

    #region PUT /api/tests/{id}

    [Fact]
    public async Task UpdateTest_WithValidData_UpdatesTest()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("Original Name")
            .WithDescription("Original description")
            .BuildAsync();

        var updateRequest = TestDataBuilder.UpdateTestRequest(
            name: "Updated Name",
            description: "Updated description",
            instructions: "Updated instructions"
        );

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tests/{test.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Clear context cache to ensure fresh data from database
        _dbFixture.Context.ChangeTracker.Clear();

        // Verify in database
        var dbTest = await _dbFixture.Context.Tests.FindAsync(test.Id);
        dbTest.Should().NotBeNull();
        dbTest!.Name.Should().Be("Updated Name");
        dbTest.Description.Should().Be("Updated description");
        dbTest.Instructions.Should().Be("Updated instructions");
        dbTest.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateTest_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = TestDataBuilder.UpdateTestRequest();

        // Act
        var response = await _client.PutAsJsonAsync("/api/tests/99999", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTest_WithOtherUsersTest_ReturnsForbidden()
    {
        // Arrange
        var otherUser = await _dbFixture.CreateTestUser("other@example.com", "Other User");
        var builder = new TestDataBuilder(_dbFixture.Context);
        var otherTest = await builder.Test(otherUser.Id).BuildAsync();

        var updateRequest = TestDataBuilder.UpdateTestRequest();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tests/{otherTest.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region DELETE /api/tests/{id}

    [Fact]
    public async Task DeleteTest_WithValidId_DeletesTest()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("Test to Delete")
            .WithDocument("document1.pdf")
            .BuildAsync();

        var testId = test.Id;

        // Act
        var response = await _client.DeleteAsync($"/api/tests/{testId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Clear context cache to ensure fresh data from database
        _dbFixture.Context.ChangeTracker.Clear();

        // Verify deleted from database
        var dbTest = await _dbFixture.Context.Tests.FindAsync(testId);
        dbTest.Should().BeNull();

        // Verify documents also deleted (cascade)
        var documents = _dbFixture.Context.StudyDocuments.Where(d => d.TestId == testId).ToList();
        documents.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteTest_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/tests/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTest_WithOtherUsersTest_ReturnsForbidden()
    {
        // Arrange
        var otherUser = await _dbFixture.CreateTestUser("other@example.com", "Other User");
        var builder = new TestDataBuilder(_dbFixture.Context);
        var otherTest = await builder.Test(otherUser.Id).BuildAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/tests/{otherTest.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
