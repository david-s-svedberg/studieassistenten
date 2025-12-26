using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StudieAssistenten.Client.Services;
using StudieAssistenten.Shared.DTOs;
using TestsPage = StudieAssistenten.Client.Pages.Tests;

namespace StudieAssistenten.Client.Tests.Pages;

/// <summary>
/// bUnit tests for Tests page component.
/// Tests page rendering, service interactions, and CRUD operations.
/// </summary>
public class TestsPageTests : TestContext
{
    private readonly Mock<ITestService> _mockTestService;
    private readonly FakeNavigationManager _fakeNavigation;

    public TestsPageTests()
    {
        _mockTestService = new Mock<ITestService>();

        // Register mocked service BEFORE getting any services
        Services.AddSingleton(_mockTestService.Object);

        _fakeNavigation = Services.GetRequiredService<FakeNavigationManager>();
    }

    [Fact]
    public void Page_WhenLoading_DisplaysSpinner()
    {
        // Arrange
        var tcs = new TaskCompletionSource<List<TestDto>>();
        _mockTestService.Setup(s => s.GetAllTestsAsync())
            .Returns(tcs.Task);

        // Act
        var cut = RenderComponent<TestsPage>();

        // Assert - Should show loading spinner
        var spinner = cut.Find(".spinner-border");
        spinner.Should().NotBeNull();
        spinner.ClassList.Should().Contain("spinner-border");
    }

    [Fact]
    public async Task Page_WhenNoTests_DisplaysEmptyState()
    {
        // Arrange
        _mockTestService.Setup(s => s.GetAllTestsAsync())
            .ReturnsAsync(new List<TestDto>());

        // Act
        var cut = RenderComponent<TestsPage>();
        await Task.Delay(50); // Allow async initialization to complete

        // Assert
        var alert = cut.Find(".alert-info");
        alert.TextContent.Should().Contain("No tests yet");
        alert.TextContent.Should().Contain("Create your first test");
    }

    [Fact]
    public async Task Page_WithTests_DisplaysTestCards()
    {
        // Arrange
        var testData = new List<TestDto>
        {
            new TestDto
            {
                Id = 1,
                Name = "Math Test",
                Description = "Math final exam",
                DocumentCount = 3,
                TotalCharacters = 5000,
                HasGeneratedContent = true,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new TestDto
            {
                Id = 2,
                Name = "History Test",
                Description = "World War II",
                DocumentCount = 1,
                TotalCharacters = 0,
                HasGeneratedContent = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        _mockTestService.Setup(s => s.GetAllTestsAsync())
            .ReturnsAsync(testData);

        // Act
        var cut = RenderComponent<TestsPage>();
        await Task.Delay(50); // Allow async initialization

        // Assert
        var cards = cut.FindAll(".card");
        cards.Should().HaveCount(2);

        // Verify first test card
        var firstCard = cards[0];
        firstCard.QuerySelector(".card-title")?.TextContent.Should().Be("Math Test");
        firstCard.QuerySelector(".card-text")?.TextContent.Should().Be("Math final exam");
        firstCard.TextContent.Should().Contain("3 documents");
        firstCard.TextContent.Should().Contain("Has Generated Content");
    }

    [Fact]
    public async Task CreateButton_WhenClicked_ShowsCreateDialog()
    {
        // Arrange
        _mockTestService.Setup(s => s.GetAllTestsAsync())
            .ReturnsAsync(new List<TestDto>());

        var cut = RenderComponent<TestsPage>();
        await Task.Delay(50);

        // Act
        var createButton = cut.Find("button.btn-primary");
        createButton.Click();

        // Assert - Dialog should be visible
        var dialog = cut.Find(".modal.show");
        dialog.Should().NotBeNull();
        dialog.QuerySelector(".modal-title")?.TextContent.Should().Be("Create New Test");
    }

    [Fact]
    public async Task CreateDialog_WhenSaved_CallsServiceAndRefreshes()
    {
        // Arrange
        _mockTestService.Setup(s => s.GetAllTestsAsync())
            .ReturnsAsync(new List<TestDto>());

        var createdTest = new TestDto
        {
            Id = 1,
            Name = "New Test",
            Description = "Test description",
            DocumentCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        _mockTestService.Setup(s => s.CreateTestAsync(It.IsAny<CreateTestRequest>()))
            .ReturnsAsync(createdTest);

        var cut = RenderComponent<TestsPage>();
        await Task.Delay(50);

        // Act - Open dialog
        var createButton = cut.Find("button.btn-primary");
        createButton.Click();

        // Fill in form
        var nameInput = cut.Find("input[type='text']");
        nameInput.Change("New Test");

        var descriptionTextarea = cut.FindAll("textarea")[0];
        descriptionTextarea.Change("Test description");

        // Click Save
        var saveButton = cut.Find(".modal-footer .btn-primary");
        saveButton.Click();

        await Task.Delay(50);

        // Assert
        _mockTestService.Verify(s => s.CreateTestAsync(It.Is<CreateTestRequest>(r =>
            r.Name == "New Test" && r.Description == "Test description")), Times.Once);

        _mockTestService.Verify(s => s.GetAllTestsAsync(), Times.AtLeast(2)); // Initial load + refresh
    }

    [Fact]
    public async Task SaveButton_WhenNameEmpty_IsDisabled()
    {
        // Arrange
        _mockTestService.Setup(s => s.GetAllTestsAsync())
            .ReturnsAsync(new List<TestDto>());

        var cut = RenderComponent<TestsPage>();
        await Task.Delay(50);

        // Act - Open dialog
        var createButton = cut.Find("button.btn-primary");
        createButton.Click();

        // Assert - Save button should be disabled when name is empty
        var saveButton = cut.Find(".modal-footer .btn-primary");
        saveButton.HasAttribute("disabled").Should().BeTrue();
    }

    [Fact]
    public async Task EditButton_WhenClicked_ShowsEditDialogWithPrefilledData()
    {
        // Arrange
        var testData = new List<TestDto>
        {
            new TestDto
            {
                Id = 1,
                Name = "Existing Test",
                Description = "Test description",
                Instructions = "Test instructions",
                DocumentCount = 2,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockTestService.Setup(s => s.GetAllTestsAsync())
            .ReturnsAsync(testData);

        var cut = RenderComponent<TestsPage>();
        await Task.Delay(50);

        // Act - Click edit button (pencil icon)
        var editButton = cut.Find(".btn-outline-secondary");
        editButton.Click();

        // Assert - Dialog should show with prefilled data
        var dialog = cut.Find(".modal.show");
        dialog.QuerySelector(".modal-title")?.TextContent.Should().Be("Edit Test");

        var nameInput = cut.Find("input[type='text']");
        nameInput.GetAttribute("value").Should().Be("Existing Test");
    }

    [Fact]
    public async Task DeleteButton_WhenClicked_CallsDeleteService()
    {
        // Arrange
        var testData = new List<TestDto>
        {
            new TestDto
            {
                Id = 1,
                Name = "Test to Delete",
                DocumentCount = 0,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockTestService.Setup(s => s.GetAllTestsAsync())
            .ReturnsAsync(testData);

        _mockTestService.Setup(s => s.DeleteTestAsync(It.IsAny<int>()))
            .ReturnsAsync(true);

        var cut = RenderComponent<TestsPage>();
        await Task.Delay(50);

        // Act - Click delete button (trash icon)
        var deleteButton = cut.Find(".btn-outline-danger");
        deleteButton.Click();

        await Task.Delay(50);

        // Assert
        _mockTestService.Verify(s => s.DeleteTestAsync(1), Times.Once);
        _mockTestService.Verify(s => s.GetAllTestsAsync(), Times.AtLeast(2)); // Initial + refresh
    }

    [Fact]
    public async Task ViewButton_WhenClicked_NavigatesToTestDetail()
    {
        // Arrange
        var testData = new List<TestDto>
        {
            new TestDto
            {
                Id = 42,
                Name = "Test to View",
                DocumentCount = 1,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockTestService.Setup(s => s.GetAllTestsAsync())
            .ReturnsAsync(testData);

        var cut = RenderComponent<TestsPage>();
        await Task.Delay(50);

        // Act - Click view button (eye icon)
        var viewButton = cut.Find(".btn-outline-primary");
        viewButton.Click();

        // Assert
        _fakeNavigation.Uri.Should().EndWith("/tests/42");
    }

    [Fact]
    public async Task CancelButton_WhenClicked_HidesDialog()
    {
        // Arrange
        _mockTestService.Setup(s => s.GetAllTestsAsync())
            .ReturnsAsync(new List<TestDto>());

        var cut = RenderComponent<TestsPage>();
        await Task.Delay(50);

        // Act - Open dialog
        var createButton = cut.Find("button.btn-primary");
        createButton.Click();

        // Verify dialog is open
        cut.FindAll(".modal.show").Should().HaveCount(1);

        // Click Cancel
        var cancelButton = cut.Find(".modal-footer .btn-secondary");
        cancelButton.Click();

        // Assert - Dialog should be hidden
        cut.FindAll(".modal.show").Should().BeEmpty();
    }

    [Fact]
    public async Task Page_WithLargeCharacterCount_FormatsCorrectly()
    {
        // Arrange
        var testData = new List<TestDto>
        {
            new TestDto
            {
                Id = 1,
                Name = "Large Test",
                DocumentCount = 10,
                TotalCharacters = 1500000, // 1.5M characters
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockTestService.Setup(s => s.GetAllTestsAsync())
            .ReturnsAsync(testData);

        // Act
        var cut = RenderComponent<TestsPage>();
        await Task.Delay(50);

        // Assert - Should format character count as "1.5M chars"
        var card = cut.Find(".card");
        card.TextContent.Should().Contain("1.5M chars");
    }
}
