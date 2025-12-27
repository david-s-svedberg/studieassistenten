using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using StudieAssistenten.Client.Services;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;
using TestDetailPage = StudieAssistenten.Client.Pages.TestDetail;

namespace StudieAssistenten.Client.Tests.Pages;

/// <summary>
/// bUnit tests for TestDetail page component.
/// Tests page rendering, service interactions, dialogs, and key user workflows.
/// </summary>
public class TestDetailPageTests : TestContext
{
    private readonly Mock<ITestService> _mockTestService;
    private readonly Mock<IDocumentService> _mockDocumentService;
    private readonly ContentGenerationService _contentService;
    private readonly Mock<IToastService> _mockToastService;
    private readonly Mock<ITestShareService> _mockTestShareService;
    private readonly FakeNavigationManager _fakeNavigation;

    public TestDetailPageTests()
    {
        _mockTestService = new Mock<ITestService>();
        _mockDocumentService = new Mock<IDocumentService>();
        _mockToastService = new Mock<IToastService>();
        _mockTestShareService = new Mock<ITestShareService>();

        // Create ContentGenerationService with mocked dependencies
        var mockHttpClient = new HttpClient(new MockHttpMessageHandler());
        var mockLogger = new Mock<ILogger<ContentGenerationService>>();
        _contentService = new ContentGenerationService(mockHttpClient, mockLogger.Object);

        // Register mocked services BEFORE getting any services
        Services.AddSingleton(_mockTestService.Object);
        Services.AddSingleton(_mockDocumentService.Object);
        Services.AddSingleton(_contentService);
        Services.AddSingleton(_mockToastService.Object);
        Services.AddSingleton(_mockTestShareService.Object);
        Services.AddSingleton(new TestStateService()); // Add TestStateService

        // Add IJSRuntime mock
        Services.AddSingleton<IJSRuntime>(new Mock<IJSRuntime>().Object);

        _fakeNavigation = Services.GetRequiredService<FakeNavigationManager>();
    }

    // Mock HTTP message handler for ContentGenerationService
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Return empty list for GetTestContentsAsync
            var response = new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public void Page_WhenLoading_DisplaysSpinner()
    {
        // Arrange
        var tcs = new TaskCompletionSource<TestDetailDto?>();
        _mockTestService.Setup(s => s.GetTestAsync(It.IsAny<int>()))
            .Returns(tcs.Task);

        // Act
        var cut = RenderComponent<TestDetailPage>(parameters => parameters
            .Add(p => p.TestId, 1));

        // Assert
        var spinner = cut.Find(".spinner-border");
        spinner.Should().NotBeNull();
    }

    [Fact]
    public async Task Page_WhenTestNotFound_DisplaysErrorMessage()
    {
        // Arrange
        _mockTestService.Setup(s => s.GetTestAsync(It.IsAny<int>()))
            .ReturnsAsync((TestDetailDto?)null);

        // Act
        var cut = RenderComponent<TestDetailPage>(parameters => parameters
            .Add(p => p.TestId, 999));
        await Task.Delay(50);

        // Assert
        var alert = cut.Find(".alert-danger");
        alert.TextContent.Should().Contain("Test not found");
        alert.TextContent.Should().Contain("doesn't exist");
    }

    [Fact]
    public async Task Page_WhenTestLoaded_DisplaysTestName()
    {
        // Arrange
        var test = new TestDetailDto
        {
            Id = 1,
            Name = "My Test",
            Description = "Test description",
            DocumentCount = 0,
            CreatedAt = DateTime.UtcNow,
            IsOwner = true
        };

        _mockTestService.Setup(s => s.GetTestAsync(1))
            .ReturnsAsync(test);
        _mockDocumentService.Setup(s => s.GetDocumentsAsync())
            .ReturnsAsync(new List<DocumentDto>());

        // Act
        var cut = RenderComponent<TestDetailPage>(parameters => parameters
            .Add(p => p.TestId, 1));
        await Task.Delay(50);

        // Assert
        var title = cut.Find(".test-title");
        title.TextContent.Should().Contain("My Test");
    }

    [Fact]
    public async Task Page_WhenTestHasDescription_DisplaysDescription()
    {
        // Arrange
        var test = new TestDetailDto
        {
            Id = 1,
            Name = "My Test",
            Description = "This is a test description",
            DocumentCount = 0,
            CreatedAt = DateTime.UtcNow,
            IsOwner = true
        };

        _mockTestService.Setup(s => s.GetTestAsync(1))
            .ReturnsAsync(test);
        _mockDocumentService.Setup(s => s.GetDocumentsAsync())
            .ReturnsAsync(new List<DocumentDto>());

        // Act
        var cut = RenderComponent<TestDetailPage>(parameters => parameters
            .Add(p => p.TestId, 1));
        await Task.Delay(50);

        // Assert
        cut.Markup.Should().Contain("This is a test description");
    }

    [Fact]
    public async Task Page_WhenNoDocuments_DisplaysEmptyState()
    {
        // Arrange
        var test = new TestDetailDto
        {
            Id = 1,
            Name = "My Test",
            DocumentCount = 0,
            Documents = new List<DocumentSummaryDto>(),
            CreatedAt = DateTime.UtcNow,
            IsOwner = true
        };

        _mockTestService.Setup(s => s.GetTestAsync(1))
            .ReturnsAsync(test);
        _mockDocumentService.Setup(s => s.GetDocumentsAsync())
            .ReturnsAsync(new List<DocumentDto>());

        // Act
        var cut = RenderComponent<TestDetailPage>(parameters => parameters
            .Add(p => p.TestId, 1));
        await Task.Delay(50);

        // Assert
        var emptyMessage = cut.Find(".no-documents-message");
        emptyMessage.TextContent.Should().Contain("No documents uploaded yet");
    }

    [Fact]
    public async Task Page_WhenHasDocuments_DisplaysDocumentCards()
    {
        // Arrange
        var documentSummaries = new List<DocumentSummaryDto>
        {
            new DocumentSummaryDto
            {
                Id = 1,
                FileName = "document1.pdf",
                FileSizeBytes = 1024000,
                Status = DocumentStatus.OcrCompleted,
                TestId = 1
            },
            new DocumentSummaryDto
            {
                Id = 2,
                FileName = "document2.jpg",
                FileSizeBytes = 512000,
                Status = DocumentStatus.OcrCompleted,
                TestId = 1
            }
        };

        var documents = new List<DocumentDto>
        {
            new DocumentDto
            {
                Id = 1,
                FileName = "document1.pdf",
                FileSizeBytes = 1024000,
                Status = DocumentStatus.OcrCompleted,
                TestId = 1
            },
            new DocumentDto
            {
                Id = 2,
                FileName = "document2.jpg",
                FileSizeBytes = 512000,
                Status = DocumentStatus.OcrCompleted,
                TestId = 1
            }
        };

        var test = new TestDetailDto
        {
            Id = 1,
            Name = "My Test",
            DocumentCount = 2,
            Documents = documentSummaries,
            CreatedAt = DateTime.UtcNow,
            IsOwner = true
        };

        _mockTestService.Setup(s => s.GetTestAsync(1))
            .ReturnsAsync(test);
        _mockDocumentService.Setup(s => s.GetDocumentsAsync())
            .ReturnsAsync(documents);

        // Act
        var cut = RenderComponent<TestDetailPage>(parameters => parameters
            .Add(p => p.TestId, 1));
        await Task.Delay(50);

        // Assert
        var documentCards = cut.FindAll(".document-card");
        documentCards.Should().HaveCount(2);
        cut.Markup.Should().Contain("document1.pdf");
        cut.Markup.Should().Contain("document2.jpg");
    }

    [Fact(Skip = "Complex to mock HTTP responses for ContentGenerationService")]
    public async Task Page_WhenHasGeneratedContent_DisplaysContentCards()
    {
        // Arrange
        var generatedContent = new List<GeneratedContentDto>
        {
            new GeneratedContentDto
            {
                Id = 1,
                Title = "Flashcards",
                ProcessingType = ProcessingType.Flashcards,
                GeneratedAt = DateTime.UtcNow
            },
            new GeneratedContentDto
            {
                Id = 2,
                Title = "Practice Test",
                ProcessingType = ProcessingType.PracticeTest,
                GeneratedAt = DateTime.UtcNow
            }
        };

        var test = new TestDetailDto
        {
            Id = 1,
            Name = "My Test",
            DocumentCount = 1,
            Documents = new List<DocumentSummaryDto>(),
            CreatedAt = DateTime.UtcNow,
            IsOwner = true
        };

        _mockTestService.Setup(s => s.GetTestAsync(1))
            .ReturnsAsync(test);
        _mockDocumentService.Setup(s => s.GetDocumentsAsync())
            .ReturnsAsync(new List<DocumentDto>());

        // Act
        var cut = RenderComponent<TestDetailPage>(parameters => parameters
            .Add(p => p.TestId, 1));
        await Task.Delay(50);

        // Assert
        var heading = cut.Find(".generated-content-section h3");
        heading.TextContent.Should().Contain("Generated Content (2)");

        var contentCards = cut.FindAll(".content-card");
        contentCards.Should().HaveCount(2);
    }

    [Fact]
    public async Task GenerateFlashcardsButton_WhenClicked_ShowsFlashcardDialog()
    {
        // Arrange
        var documentSummaries = new List<DocumentSummaryDto>
        {
            new DocumentSummaryDto
            {
                Id = 1,
                FileName = "doc.pdf",
                Status = DocumentStatus.OcrCompleted,
                TestId = 1
            }
        };

        var documents = new List<DocumentDto>
        {
            new DocumentDto
            {
                Id = 1,
                FileName = "doc.pdf",
                Status = DocumentStatus.OcrCompleted,
                ExtractedText = "Some text",
                TestId = 1
            }
        };

        var test = new TestDetailDto
        {
            Id = 1,
            Name = "My Test",
            DocumentCount = 1,
            Documents = documentSummaries,
            CreatedAt = DateTime.UtcNow,
            IsOwner = true
        };

        _mockTestService.Setup(s => s.GetTestAsync(1))
            .ReturnsAsync(test);
        _mockDocumentService.Setup(s => s.GetDocumentsAsync())
            .ReturnsAsync(documents);

        var cut = RenderComponent<TestDetailPage>(parameters => parameters
            .Add(p => p.TestId, 1));
        await Task.Delay(50);

        // Act
        var flashcardButton = cut.Find(".btn-success");
        flashcardButton.Click();

        // Assert
        var dialog = cut.Find(".modal");
        dialog.Should().NotBeNull();
        var dialogTitle = dialog.QuerySelector(".modal-title");
        dialogTitle?.TextContent.Should().Contain("Flashcard Options");
    }

    [Fact]
    public async Task GeneratePracticeTestButton_WhenClicked_ShowsPracticeTestDialog()
    {
        // Arrange
        var documentSummaries = new List<DocumentSummaryDto>
        {
            new DocumentSummaryDto
            {
                Id = 1,
                FileName = "doc.pdf",
                Status = DocumentStatus.OcrCompleted,
                TestId = 1
            }
        };

        var documents = new List<DocumentDto>
        {
            new DocumentDto
            {
                Id = 1,
                FileName = "doc.pdf",
                Status = DocumentStatus.OcrCompleted,
                ExtractedText = "Some text",
                TestId = 1
            }
        };

        var test = new TestDetailDto
        {
            Id = 1,
            Name = "My Test",
            DocumentCount = 1,
            Documents = documentSummaries,
            CreatedAt = DateTime.UtcNow,
            IsOwner = true
        };

        _mockTestService.Setup(s => s.GetTestAsync(1))
            .ReturnsAsync(test);
        _mockDocumentService.Setup(s => s.GetDocumentsAsync())
            .ReturnsAsync(documents);

        var cut = RenderComponent<TestDetailPage>(parameters => parameters
            .Add(p => p.TestId, 1));
        await Task.Delay(50);

        // Act
        var practiceTestButton = cut.Find(".btn-info");
        practiceTestButton.Click();

        // Assert
        var dialog = cut.Find(".modal");
        dialog.Should().NotBeNull();
        var dialogTitle = dialog.QuerySelector(".modal-title");
        dialogTitle?.TextContent.Should().Contain("Practice Test Options");
    }

    [Fact]
    public async Task GenerateSummaryButton_WhenClicked_ShowsSummaryDialog()
    {
        // Arrange
        var documentSummaries = new List<DocumentSummaryDto>
        {
            new DocumentSummaryDto
            {
                Id = 1,
                FileName = "doc.pdf",
                Status = DocumentStatus.OcrCompleted,
                TestId = 1
            }
        };

        var documents = new List<DocumentDto>
        {
            new DocumentDto
            {
                Id = 1,
                FileName = "doc.pdf",
                Status = DocumentStatus.OcrCompleted,
                ExtractedText = "Some text",
                TestId = 1
            }
        };

        var test = new TestDetailDto
        {
            Id = 1,
            Name = "My Test",
            DocumentCount = 1,
            Documents = documentSummaries,
            CreatedAt = DateTime.UtcNow,
            IsOwner = true
        };

        _mockTestService.Setup(s => s.GetTestAsync(1))
            .ReturnsAsync(test);
        _mockDocumentService.Setup(s => s.GetDocumentsAsync())
            .ReturnsAsync(documents);

        var cut = RenderComponent<TestDetailPage>(parameters => parameters
            .Add(p => p.TestId, 1));
        await Task.Delay(50);

        // Act
        var summaryButton = cut.Find(".btn-warning");
        summaryButton.Click();

        // Assert
        var dialog = cut.Find(".modal");
        dialog.Should().NotBeNull();
        var dialogTitle = dialog.QuerySelector(".modal-title");
        dialogTitle?.TextContent.Should().Contain("Summary Options");
    }

    [Fact]
    public async Task UploadModeSwitch_WhenClickingTextTab_ShowsTextInput()
    {
        // Arrange
        var test = new TestDetailDto
        {
            Id = 1,
            Name = "My Test",
            DocumentCount = 0,
            Documents = new List<DocumentSummaryDto>(),
            CreatedAt = DateTime.UtcNow,
            IsOwner = true
        };

        _mockTestService.Setup(s => s.GetTestAsync(1))
            .ReturnsAsync(test);
        _mockDocumentService.Setup(s => s.GetDocumentsAsync())
            .ReturnsAsync(new List<DocumentDto>());

        var cut = RenderComponent<TestDetailPage>(parameters => parameters
            .Add(p => p.TestId, 1));
        await Task.Delay(50);

        // Act
        var tabs = cut.FindAll(".tab-btn");
        var textTab = tabs.First(t => t.TextContent.Contains("Enter Text"));
        textTab.Click();

        // Assert
        var textArea = cut.Find(".text-input-box");
        textArea.Should().NotBeNull();
        textArea.GetAttribute("placeholder").Should().Contain("Type or paste");
    }

    [Fact]
    public async Task GenerationButtons_WhenNoDocuments_AreNotVisible()
    {
        // Arrange
        var test = new TestDetailDto
        {
            Id = 1,
            Name = "My Test",
            DocumentCount = 0,
            Documents = new List<DocumentSummaryDto>(),
            CreatedAt = DateTime.UtcNow,
            IsOwner = true
        };

        _mockTestService.Setup(s => s.GetTestAsync(1))
            .ReturnsAsync(test);
        _mockDocumentService.Setup(s => s.GetDocumentsAsync())
            .ReturnsAsync(new List<DocumentDto>());

        // Act
        var cut = RenderComponent<TestDetailPage>(parameters => parameters
            .Add(p => p.TestId, 1));
        await Task.Delay(50);

        // Assert
        var generationButtons = cut.FindAll(".generation-buttons");
        generationButtons.Should().BeEmpty();
    }
}
