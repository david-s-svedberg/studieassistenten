using System.Net;
using System.Net.Http.Headers;
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
/// Integration tests for DocumentsController.
/// Tests file upload, retrieval, deletion, and authorization.
/// </summary>
[Collection("Sequential")]
public class DocumentsControllerTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client;
    private ApplicationUser _testUser = null!;
    private DatabaseFixture _dbFixture = null!;

    public DocumentsControllerTests(TestWebApplicationFactory factory)
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

    #region POST /api/documents/upload

    [Fact]
    public async Task UploadDocument_WithValidPdf_UploadsSuccessfully()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id).WithName("Test for Upload").BuildAsync();

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(CreateFakePdfBytes());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "test-document.pdf");
        content.Add(new StringContent(test.Id.ToString()), "testId");

        // Act
        var response = await _client.PostAsync("/api/documents/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await response.Content.ReadFromJsonAsync<DocumentDto>();
        document.Should().NotBeNull();
        document!.FileName.Should().Be("test-document.pdf");
        document.ContentType.Should().Be("application/pdf");
        document.TestId.Should().Be(test.Id);
    }

    [Fact]
    public async Task UploadDocument_WithNoFile_ReturnsBadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("1"), "testId");

        // Act
        var response = await _client.PostAsync("/api/documents/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadDocument_WithEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "empty.pdf");
        content.Add(new StringContent("1"), "testId");

        // Act
        var response = await _client.PostAsync("/api/documents/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("No file uploaded");
    }

    [Fact]
    public async Task UploadDocument_WithUnsupportedFileType_ReturnsBadRequest()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/exe");
        content.Add(fileContent, "file", "malware.exe");
        content.Add(new StringContent("1"), "testId");

        // Act
        var response = await _client.PostAsync("/api/documents/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadAsStringAsync();
        error.Should().Contain("not supported");
    }

    #endregion

    #region GET /api/documents

    [Fact]
    public async Task GetAllDocuments_WhenNoDocuments_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/documents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var documents = await response.Content.ReadFromJsonAsync<List<DocumentSummaryDto>>();
        documents.Should().NotBeNull();
        documents.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllDocuments_WithMultipleDocuments_ReturnsAllUserDocuments()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("Test with Documents")
            .WithDocument("doc1.pdf")
            .WithDocument("doc2.pdf")
            .WithDocument("doc3.pdf")
            .BuildAsync();

        // Create a document for a different user (should not be returned)
        var otherUser = await _dbFixture.CreateTestUser("other@example.com", "Other User");
        await builder.Test(otherUser.Id)
            .WithName("Other User Test")
            .WithDocument("other-doc.pdf")
            .BuildAsync();

        // Act
        var response = await _client.GetAsync("/api/documents");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var documents = await response.Content.ReadFromJsonAsync<List<DocumentSummaryDto>>();
        documents.Should().NotBeNull();
        documents.Should().HaveCount(3);
        documents.Should().OnlyContain(d => d.FileName.StartsWith("doc"));
    }

    #endregion

    #region GET /api/documents/{id}

    [Fact]
    public async Task GetDocument_WithValidId_ReturnsDocument()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("Test")
            .WithDocument("sample.pdf", "Sample extracted text")
            .BuildAsync();

        var document = test.Documents.First();

        // Act
        var response = await _client.GetAsync($"/api/documents/{document.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var documentDto = await response.Content.ReadFromJsonAsync<DocumentDetailDto>();
        documentDto.Should().NotBeNull();
        documentDto!.Id.Should().Be(document.Id);
        documentDto.FileName.Should().Be("sample.pdf");
        documentDto.ExtractedText.Should().Be("Sample extracted text");
    }

    [Fact]
    public async Task GetDocument_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/documents/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDocument_WithOtherUsersDocument_ReturnsNotFound()
    {
        // Arrange
        var otherUser = await _dbFixture.CreateTestUser("other@example.com", "Other User");
        var builder = new TestDataBuilder(_dbFixture.Context);
        var otherTest = await builder.Test(otherUser.Id)
            .WithName("Other Test")
            .WithDocument("other-doc.pdf")
            .BuildAsync();

        var otherDocument = otherTest.Documents.First();

        // Act
        var response = await _client.GetAsync($"/api/documents/{otherDocument.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/documents/{id}

    [Fact]
    public async Task DeleteDocument_WithValidId_DeletesDocument()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("Test")
            .WithDocument("to-delete.pdf")
            .BuildAsync();

        var document = test.Documents.First();
        var documentId = document.Id;

        // Act
        var response = await _client.DeleteAsync($"/api/documents/{documentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Clear context cache to ensure fresh data from database
        _dbFixture.Context.ChangeTracker.Clear();

        // Verify deleted from database
        var dbDocument = await _dbFixture.Context.StudyDocuments.FindAsync(documentId);
        dbDocument.Should().BeNull();
    }

    [Fact]
    public async Task DeleteDocument_WithNonExistentId_ReturnsNotFound()
    {
        // Act
        var response = await _client.DeleteAsync("/api/documents/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteDocument_WithOtherUsersDocument_ReturnsNotFound()
    {
        // Arrange
        var otherUser = await _dbFixture.CreateTestUser("other@example.com", "Other User");
        var builder = new TestDataBuilder(_dbFixture.Context);
        var otherTest = await builder.Test(otherUser.Id)
            .WithName("Other Test")
            .WithDocument("other-doc.pdf")
            .BuildAsync();

        var otherDocument = otherTest.Documents.First();

        // Act
        var response = await _client.DeleteAsync($"/api/documents/{otherDocument.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private static byte[] CreateFakePdfBytes()
    {
        // PDF magic bytes: %PDF-1.4
        var pdfHeader = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        var content = new byte[1024];
        Array.Copy(pdfHeader, content, pdfHeader.Length);
        return content;
    }

    #endregion
}
