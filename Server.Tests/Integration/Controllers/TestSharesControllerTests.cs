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
/// Integration tests for TestSharesController.
/// Tests test sharing functionality with authorization.
/// </summary>
[Collection("Sequential")]
public class TestSharesControllerTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client;
    private ApplicationUser _testUser = null!;
    private ApplicationUser _otherUser = null!;
    private DatabaseFixture _dbFixture = null!;

    public TestSharesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        _dbFixture = new DatabaseFixture(_factory.Services);

        _testUser = await _dbFixture.CreateTestUser();
        _otherUser = await _dbFixture.CreateTestUser("other@example.com", "Other User");

        _client.Dispose();
        _client = AuthenticationFixture.CreateAuthenticatedClient(_factory, _testUser);
    }

    public async Task DisposeAsync()
    {
        await _dbFixture.ClearDatabase();
        _dbFixture.Dispose();
        _client.Dispose();
    }

    #region POST /api/testshares

    [Fact]
    public async Task ShareTest_WithValidRequest_CreatesShare()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("My Test")
            .BuildAsync();

        var request = new CreateTestShareRequest
        {
            TestId = test.Id,
            SharedWithEmail = _otherUser.Email!
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/testshares", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var share = await response.Content.ReadFromJsonAsync<TestShareDto>();
        share.Should().NotBeNull();
        share!.TestId.Should().Be(test.Id);
        share.TestName.Should().Be("My Test");
        share.SharedWith.Email.Should().Be(_otherUser.Email);
        share.Owner.Email.Should().Be(_testUser.Email);
        share.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ShareTest_WithNonExistentUser_ReturnsBadRequest()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("My Test")
            .BuildAsync();

        var request = new CreateTestShareRequest
        {
            TestId = test.Id,
            SharedWithEmail = "nonexistent@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/testshares", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShareTest_WithOwnEmail_ReturnsBadRequest()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("My Test")
            .BuildAsync();

        var request = new CreateTestShareRequest
        {
            TestId = test.Id,
            SharedWithEmail = _testUser.Email!
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/testshares", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShareTest_WithOtherUsersTest_ReturnsForbidden()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var otherTest = await builder.Test(_otherUser.Id)
            .WithName("Other Test")
            .BuildAsync();

        var request = new CreateTestShareRequest
        {
            TestId = otherTest.Id,
            SharedWithEmail = "someone@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/testshares", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShareTest_DuplicateShare_ReturnsExistingShare()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("My Test")
            .BuildAsync();

        // Create initial share
        var share1 = new TestShare
        {
            TestId = test.Id,
            OwnerId = _testUser.Id,
            SharedWithUserId = _otherUser.Id,
            SharedAt = DateTime.UtcNow
        };
        _dbFixture.Context.TestShares.Add(share1);
        await _dbFixture.Context.SaveChangesAsync();

        var request = new CreateTestShareRequest
        {
            TestId = test.Id,
            SharedWithEmail = _otherUser.Email!
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/testshares", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var share = await response.Content.ReadFromJsonAsync<TestShareDto>();
        share.Should().NotBeNull();
        share!.Id.Should().Be(share1.Id); // Should return existing share
    }

    #endregion

    #region GET /api/testshares/test/{testId}

    [Fact]
    public async Task GetSharesForTest_AsOwner_ReturnsShares()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("My Test")
            .BuildAsync();

        var user2 = await _dbFixture.CreateTestUser("user2@example.com", "User 2");
        var user3 = await _dbFixture.CreateTestUser("user3@example.com", "User 3");

        // Create shares
        _dbFixture.Context.TestShares.AddRange(
            new TestShare
            {
                TestId = test.Id,
                OwnerId = _testUser.Id,
                SharedWithUserId = user2.Id,
                SharedAt = DateTime.UtcNow
            },
            new TestShare
            {
                TestId = test.Id,
                OwnerId = _testUser.Id,
                SharedWithUserId = user3.Id,
                SharedAt = DateTime.UtcNow
            }
        );
        await _dbFixture.Context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/testshares/test/{test.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shares = await response.Content.ReadFromJsonAsync<List<TestShareDto>>();
        shares.Should().NotBeNull();
        shares.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSharesForTest_AsNonOwner_ReturnsEmpty()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_otherUser.Id)
            .WithName("Other Test")
            .BuildAsync();

        // Act
        var response = await _client.GetAsync($"/api/testshares/test/{test.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shares = await response.Content.ReadFromJsonAsync<List<TestShareDto>>();
        shares.Should().NotBeNull();
        shares.Should().BeEmpty();
    }

    #endregion

    #region GET /api/testshares/user

    [Fact]
    public async Task GetSharesForUser_ReturnsTestsSharedWithUser()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test1 = await builder.Test(_otherUser.Id).WithName("Shared Test 1").BuildAsync();
        var test2 = await builder.Test(_otherUser.Id).WithName("Shared Test 2").BuildAsync();

        // Share tests with the current user
        _dbFixture.Context.TestShares.AddRange(
            new TestShare
            {
                TestId = test1.Id,
                OwnerId = _otherUser.Id,
                SharedWithUserId = _testUser.Id,
                SharedAt = DateTime.UtcNow
            },
            new TestShare
            {
                TestId = test2.Id,
                OwnerId = _otherUser.Id,
                SharedWithUserId = _testUser.Id,
                SharedAt = DateTime.UtcNow
            }
        );
        await _dbFixture.Context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/testshares/user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var shares = await response.Content.ReadFromJsonAsync<List<TestShareDto>>();
        shares.Should().NotBeNull();
        shares.Should().HaveCount(2);
        shares.Should().Contain(s => s.TestName == "Shared Test 1");
        shares.Should().Contain(s => s.TestName == "Shared Test 2");
    }

    #endregion

    #region DELETE /api/testshares/{id}

    [Fact]
    public async Task RevokeShare_AsOwner_RevokesShare()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("My Test")
            .BuildAsync();

        var share = new TestShare
        {
            TestId = test.Id,
            OwnerId = _testUser.Id,
            SharedWithUserId = _otherUser.Id,
            SharedAt = DateTime.UtcNow
        };
        _dbFixture.Context.TestShares.Add(share);
        await _dbFixture.Context.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/testshares/{share.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify share is revoked (need to reload from database)
        _dbFixture.Context.Entry(share).Reload();
        share.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeShare_AsNonOwner_ReturnsForbidden()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_otherUser.Id)
            .WithName("Other Test")
            .BuildAsync();

        var share = new TestShare
        {
            TestId = test.Id,
            OwnerId = _otherUser.Id,
            SharedWithUserId = _testUser.Id,
            SharedAt = DateTime.UtcNow
        };
        _dbFixture.Context.TestShares.Add(share);
        await _dbFixture.Context.SaveChangesAsync();

        // Act
        var response = await _client.DeleteAsync($"/api/testshares/{share.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task SharedUser_CanAccessTest_ReadOnly()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_otherUser.Id)
            .WithName("Shared Test")
            .WithDocument("document1.pdf")
            .BuildAsync();

        // Share test with current user
        var share = new TestShare
        {
            TestId = test.Id,
            OwnerId = _otherUser.Id,
            SharedWithUserId = _testUser.Id,
            SharedAt = DateTime.UtcNow
        };
        _dbFixture.Context.TestShares.Add(share);
        await _dbFixture.Context.SaveChangesAsync();

        // Act - Try to read the test
        var getResponse = await _client.GetAsync($"/api/tests/{test.Id}");

        // Assert - Should be able to read
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Try to update the test
        var updateRequest = new CreateTestRequest
        {
            Name = "Modified Name",
            Description = "Modified"
        };
        var putResponse = await _client.PutAsJsonAsync($"/api/tests/{test.Id}", updateRequest);

        // Assert - Should not be able to update
        putResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SharedUser_CanAccessDocuments_ReadOnly()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_otherUser.Id)
            .WithName("Shared Test")
            .WithDocument("document1.pdf")
            .BuildAsync();

        // Share test with current user
        var share = new TestShare
        {
            TestId = test.Id,
            OwnerId = _otherUser.Id,
            SharedWithUserId = _testUser.Id,
            SharedAt = DateTime.UtcNow
        };
        _dbFixture.Context.TestShares.Add(share);
        await _dbFixture.Context.SaveChangesAsync();

        var document = test.Documents.First();

        // Act - Try to read the test (which includes documents)
        var getResponse = await _client.GetAsync($"/api/tests/{test.Id}");

        // Assert - Should be able to read test and see documents
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var testDetail = await getResponse.Content.ReadFromJsonAsync<TestDetailDto>();
        testDetail.Should().NotBeNull();
        testDetail!.Documents.Should().HaveCount(1);
    }

    [Fact]
    public async Task RevokedShare_DeniesAccess()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_otherUser.Id)
            .WithName("Previously Shared Test")
            .BuildAsync();

        // Create revoked share
        var share = new TestShare
        {
            TestId = test.Id,
            OwnerId = _otherUser.Id,
            SharedWithUserId = _testUser.Id,
            SharedAt = DateTime.UtcNow.AddDays(-2),
            RevokedAt = DateTime.UtcNow.AddDays(-1)
        };
        _dbFixture.Context.TestShares.Add(share);
        await _dbFixture.Context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/tests/{test.Id}");

        // Assert - Should not have access
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
