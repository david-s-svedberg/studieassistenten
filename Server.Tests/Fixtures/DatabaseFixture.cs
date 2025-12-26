using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Tests.Fixtures;

/// <summary>
/// Provides database utilities for integration tests.
/// Includes methods for seeding test data and cleaning up between tests.
/// </summary>
public class DatabaseFixture
{
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _context;

    public DatabaseFixture(IServiceProvider serviceProvider)
    {
        _scope = serviceProvider.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public ApplicationDbContext Context => _context;

    /// <summary>
    /// Creates a test user in the database
    /// </summary>
    public async Task<ApplicationUser> CreateTestUser(string email = "test@example.com", string name = "Test User")
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpper(),
            NormalizedUserName = email.ToUpper(),
            FullName = name,
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Creates a test with optional documents
    /// </summary>
    public async Task<Test> CreateTestWithDocuments(
        string userId,
        string testName = "Test Integration Test",
        int documentCount = 0)
    {
        var test = new Test
        {
            Name = testName,
            Description = "Test description for integration testing",
            Instructions = "Test instructions",
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Tests.Add(test);
        await _context.SaveChangesAsync();

        // Add documents if requested
        for (int i = 0; i < documentCount; i++)
        {
            var document = new StudyDocument
            {
                TestId = test.Id,
                FileName = $"document{i + 1}.pdf",
                OriginalFilePath = $"/uploads/test-{test.Id}/document{i + 1}.pdf",
                ContentType = "application/pdf",
                FileSizeBytes = 1024 * 100, // 100 KB
                ExtractedText = $"Sample extracted text from document {i + 1}. This is test content.",
                Status = DocumentStatus.OcrCompleted,
                UploadedAt = DateTime.UtcNow
            };

            _context.StudyDocuments.Add(document);
        }

        await _context.SaveChangesAsync();

        // Reload test with documents
        return await _context.Tests
            .Include(t => t.Documents)
            .FirstAsync(t => t.Id == test.Id);
    }

    /// <summary>
    /// Creates generated content for a test
    /// </summary>
    public async Task<GeneratedContent> CreateGeneratedContent(
        int testId,
        ProcessingType processingType = ProcessingType.Flashcards,
        string? title = null)
    {
        var content = new GeneratedContent
        {
            TestId = testId,
            ProcessingType = processingType,
            Title = title ?? $"Generated {processingType}",
            Content = "Sample generated content",
            GeneratedAt = DateTime.UtcNow
        };

        _context.GeneratedContents.Add(content);
        await _context.SaveChangesAsync();

        return content;
    }

    /// <summary>
    /// Clears all data from the database (for test isolation)
    /// </summary>
    public async Task ClearDatabase()
    {
        // Use raw SQL for more reliable cleanup
        // Foreign key constraints require deletion in correct order
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Flashcards");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM GeneratedContents");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM StudyDocuments");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM Tests");
        await _context.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUsers");
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}
