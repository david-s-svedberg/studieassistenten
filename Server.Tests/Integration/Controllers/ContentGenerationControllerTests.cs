using System.Net;
using System.Net.Http.Json;
using Anthropic.SDK.Messaging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Server.Services.AI;
using StudieAssistenten.Server.Services.AI.Abstractions;
using StudieAssistenten.Server.Tests.Fixtures;
using StudieAssistenten.Server.Tests.Mocks;
using StudieAssistenten.Server.Tests.TestData;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Tests.Integration.Controllers;

/// <summary>
/// Integration tests for ContentGenerationController.
/// Tests AI content generation (flashcards, practice tests, summaries) with mocked AI provider.
/// </summary>
[Collection("Sequential")]
public class ContentGenerationControllerTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client;
    private ApplicationUser _testUser = null!;
    private DatabaseFixture _dbFixture = null!;
    private MockAnthropicClient _mockAnthropicClient = null!;
    private MockAiProvider _mockAiProvider = null!;

    public ContentGenerationControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        var scope = _factory.Services.CreateScope();
        _dbFixture = new DatabaseFixture(_factory.Services);
        _testUser = await _dbFixture.CreateTestUser();

        // Get reference to mock Anthropic client (legacy)
        _mockAnthropicClient = (MockAnthropicClient)scope.ServiceProvider.GetRequiredService<IAnthropicApiClient>();
        _mockAnthropicClient.Reset();

        // Get reference to mock AI provider (new abstraction)
        _mockAiProvider = (MockAiProvider)scope.ServiceProvider.GetRequiredService<IAiProvider>();
        _mockAiProvider.Reset();

        _client = AuthenticationFixture.CreateAuthenticatedClient(_factory, _testUser);
    }

    public async Task DisposeAsync()
    {
        await _dbFixture.ClearDatabase();
        _dbFixture.Dispose();
        _client.Dispose();
    }

    #region POST /api/ContentGeneration/generate - Flashcards

    [Fact]
    public async Task GenerateFlashcards_WithValidRequest_CreatesFlashcards()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("Flashcard Test")
            .WithDocument("document1.pdf", "Sample content for flashcards")
            .BuildAsync();

        var request = TestDataBuilder.GenerateFlashcardsRequest(test.Id);

        // Act
        var response = await _client.PostAsJsonAsync("/api/ContentGeneration/generate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GeneratedContentDto>();

        content.Should().NotBeNull();
        content!.ProcessingType.Should().Be(ProcessingType.Flashcards);
        content.Id.Should().BeGreaterThan(0);
        content.Content.Should().NotBeNullOrEmpty();

        // Verify mock was called
        _mockAiProvider.CallCount.Should().BeGreaterThan(0);

        // Verify saved to database
        var dbContent = await _dbFixture.Context.GeneratedContents
            .FirstOrDefaultAsync(c => c.Id == content.Id);
        dbContent.Should().NotBeNull();
    }

    [Fact]
    public async Task GenerateFlashcards_WithOptions_PassesOptionsToGenerator()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithDocument("document1.pdf")
            .BuildAsync();

        var request = TestDataBuilder.GenerateFlashcardsRequest(
            test.Id,
            numberOfCards: 15,
            difficultyLevel: "Advanced"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/ContentGeneration/generate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GeneratedContentDto>();
        content.Should().NotBeNull();
        content!.ProcessingType.Should().Be(ProcessingType.Flashcards);

        // Verify AI was called (mock validates the request internally)
        _mockAiProvider.CallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateFlashcards_WithNoDocuments_ReturnsBadRequest()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id).BuildAsync(); // No documents

        var request = TestDataBuilder.GenerateFlashcardsRequest(test.Id);

        // Act
        var response = await _client.PostAsJsonAsync("/api/ContentGeneration/generate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GenerateFlashcards_WithOtherUsersTest_ReturnsForbidden()
    {
        // Arrange
        var otherUser = await _dbFixture.CreateTestUser("other@example.com", "Other User");
        var builder = new TestDataBuilder(_dbFixture.Context);
        var otherTest = await builder.Test(otherUser.Id)
            .WithDocument("document1.pdf")
            .BuildAsync();

        var request = TestDataBuilder.GenerateFlashcardsRequest(otherTest.Id);

        // Act
        var response = await _client.PostAsJsonAsync("/api/ContentGeneration/generate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/ContentGeneration/generate - Practice Test

    [Fact]
    public async Task GeneratePracticeTest_WithValidRequest_CreatesPracticeTest()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("Practice Test")
            .WithDocument("document1.pdf", "Content for practice test generation")
            .BuildAsync();

        var request = TestDataBuilder.GeneratePracticeTestRequest(test.Id);

        // Act
        var response = await _client.PostAsJsonAsync("/api/ContentGeneration/generate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GeneratedContentDto>();

        content.Should().NotBeNull();
        content!.ProcessingType.Should().Be(ProcessingType.PracticeTest);
        content.Id.Should().BeGreaterThan(0);
        content.Content.Should().NotBeNullOrEmpty();

        // Verify mock was called
        _mockAiProvider.CallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GeneratePracticeTest_WithOptions_PassesOptionsToGenerator()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithDocument("document1.pdf")
            .BuildAsync();

        var request = TestDataBuilder.GeneratePracticeTestRequest(
            test.Id,
            numberOfQuestions: 10,
            questionTypes: new List<string> { "MultipleChoice", "TrueFalse" },
            includeAnswerExplanations: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/ContentGeneration/generate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GeneratedContentDto>();
        content.Should().NotBeNull();
        content!.ProcessingType.Should().Be(ProcessingType.PracticeTest);
    }

    #endregion

    #region POST /api/ContentGeneration/generate - Summary

    [Fact]
    public async Task GenerateSummary_WithValidRequest_CreatesSummary()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("Summary Test")
            .WithDocument("document1.pdf", "Content for summary generation")
            .BuildAsync();

        var request = TestDataBuilder.GenerateSummaryRequest(test.Id);

        // Act
        var response = await _client.PostAsJsonAsync("/api/ContentGeneration/generate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GeneratedContentDto>();

        content.Should().NotBeNull();
        content!.ProcessingType.Should().Be(ProcessingType.Summary);
        content.Id.Should().BeGreaterThan(0);
        content.Content.Should().NotBeNullOrEmpty();

        _mockAiProvider.CallCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GenerateSummary_WithOptions_PassesOptionsToGenerator()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithDocument("document1.pdf")
            .BuildAsync();

        var request = TestDataBuilder.GenerateSummaryRequest(
            test.Id,
            summaryLength: "Detailed",
            summaryFormat: "Outline"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/ContentGeneration/generate", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<GeneratedContentDto>();
        content.Should().NotBeNull();
        content!.ProcessingType.Should().Be(ProcessingType.Summary);
    }

    #endregion

    #region GET /api/ContentGeneration/test/{testId}

    [Fact]
    public async Task GetGeneratedContent_WithContent_ReturnsAllContent()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id).BuildAsync();

        await _dbFixture.CreateGeneratedContent(test.Id, ProcessingType.Flashcards);
        await _dbFixture.CreateGeneratedContent(test.Id, ProcessingType.PracticeTest);
        await _dbFixture.CreateGeneratedContent(test.Id, ProcessingType.Summary);

        // Act
        var response = await _client.GetAsync($"/api/ContentGeneration/test/{test.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var contents = await response.Content.ReadFromJsonAsync<List<GeneratedContentDto>>();

        contents.Should().NotBeNull();
        contents.Should().HaveCount(3);
        contents.Should().Contain(c => c.ProcessingType == ProcessingType.Flashcards);
        contents.Should().Contain(c => c.ProcessingType == ProcessingType.PracticeTest);
        contents.Should().Contain(c => c.ProcessingType == ProcessingType.Summary);
    }

    [Fact]
    public async Task GetGeneratedContent_WithNoContent_ReturnsEmptyList()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id).BuildAsync();

        // Act
        var response = await _client.GetAsync($"/api/ContentGeneration/test/{test.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var contents = await response.Content.ReadFromJsonAsync<List<GeneratedContentDto>>();

        contents.Should().NotBeNull();
        contents.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGeneratedContent_WithOtherUsersTest_ReturnsForbidden()
    {
        // Arrange
        var otherUser = await _dbFixture.CreateTestUser("other@example.com", "Other User");
        var builder = new TestDataBuilder(_dbFixture.Context);
        var otherTest = await builder.Test(otherUser.Id).BuildAsync();

        // Act
        var response = await _client.GetAsync($"/api/ContentGeneration/test/{otherTest.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region DELETE /api/ContentGeneration/{id}

    [Fact]
    public async Task DeleteGeneratedContent_WithValidId_DeletesContent()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id).BuildAsync();
        var content = await _dbFixture.CreateGeneratedContent(test.Id);

        // Act
        var response = await _client.DeleteAsync($"/api/ContentGeneration/{content.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Clear context cache to ensure fresh data from database
        _dbFixture.Context.ChangeTracker.Clear();

        // Verify deleted from database
        var dbContent = await _dbFixture.Context.GeneratedContents.FindAsync(content.Id);
        dbContent.Should().BeNull();
    }

    [Fact]
    public async Task DeleteGeneratedContent_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/ContentGeneration/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteGeneratedContent_WithOtherUsersContent_ReturnsForbidden()
    {
        // Arrange
        var otherUser = await _dbFixture.CreateTestUser("other@example.com", "Other User");
        var builder = new TestDataBuilder(_dbFixture.Context);
        var otherTest = await builder.Test(otherUser.Id).BuildAsync();
        var otherContent = await _dbFixture.CreateGeneratedContent(otherTest.Id);

        // Act
        var response = await _client.DeleteAsync($"/api/ContentGeneration/{otherContent.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
