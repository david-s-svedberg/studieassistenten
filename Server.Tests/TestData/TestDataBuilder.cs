using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.DTOs;
using StudieAssistenten.Shared.Enums;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Tests.TestData;

/// <summary>
/// Fluent builder for creating test data in integration tests.
/// Provides convenient methods for building complex test scenarios.
/// </summary>
public class TestDataBuilder
{
    private readonly ApplicationDbContext _context;

    public TestDataBuilder(ApplicationDbContext context)
    {
        _context = context;
    }

    #region User Building

    public UserBuilder User() => new UserBuilder(_context);

    public class UserBuilder
    {
        private readonly ApplicationDbContext _context;
        private string _email = "test@example.com";
        private string _name = "Test User";
        private string? _profilePictureUrl = null;

        public UserBuilder(ApplicationDbContext context)
        {
            _context = context;
        }

        public UserBuilder WithEmail(string email)
        {
            _email = email;
            return this;
        }

        public UserBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public UserBuilder WithProfilePicture(string url)
        {
            _profilePictureUrl = url;
            return this;
        }

        public async Task<ApplicationUser> BuildAsync()
        {
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = _email,
                UserName = _email,
                NormalizedEmail = _email.ToUpper(),
                NormalizedUserName = _email.ToUpper(),
                FullName = _name,
                ProfilePictureUrl = _profilePictureUrl,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }
    }

    #endregion

    #region Test Building

    public TestBuilder Test(string userId) => new TestBuilder(_context, userId);

    public class TestBuilder
    {
        private readonly ApplicationDbContext _context;
        private readonly string _userId;
        private string _name = "Test";
        private string _description = "Test description";
        private string _instructions = "";
        private readonly List<StudyDocument> _documents = new();

        public TestBuilder(ApplicationDbContext context, string userId)
        {
            _context = context;
            _userId = userId;
        }

        public TestBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public TestBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        public TestBuilder WithInstructions(string instructions)
        {
            _instructions = instructions;
            return this;
        }

        public TestBuilder WithDocument(string fileName, string extractedText = "Sample text")
        {
            var document = new StudyDocument
            {
                FileName = fileName,
                OriginalFilePath = $"/uploads/{fileName}",
                ContentType = "application/pdf",
                FileSizeBytes = 1024 * 100,
                ExtractedText = extractedText,
                Status = DocumentStatus.OcrCompleted,
                UploadedAt = DateTime.UtcNow
            };

            _documents.Add(document);
            return this;
        }

        public async Task<Test> BuildAsync()
        {
            var test = new Test
            {
                Name = _name,
                Description = _description,
                Instructions = _instructions,
                UserId = _userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tests.Add(test);
            await _context.SaveChangesAsync();

            // Add documents
            foreach (var doc in _documents)
            {
                doc.TestId = test.Id;
                _context.StudyDocuments.Add(doc);
            }

            if (_documents.Any())
            {
                await _context.SaveChangesAsync();
            }

            return test;
        }
    }

    #endregion

    #region DTO Builders (for request payloads)

    public static CreateTestRequest CreateTestRequest(
        string name = "Integration Test",
        string description = "Test description",
        string instructions = "")
    {
        return new CreateTestRequest
        {
            Name = name,
            Description = description,
            Instructions = instructions
        };
    }

    public static CreateTestRequest UpdateTestRequest(
        string name = "Updated Test",
        string description = "Updated description",
        string instructions = "Updated instructions")
    {
        return new CreateTestRequest
        {
            Name = name,
            Description = description,
            Instructions = instructions
        };
    }

    public static GenerateContentRequestDto GenerateFlashcardsRequest(
        int testId,
        int? numberOfCards = null,
        string? difficultyLevel = null)
    {
        return new GenerateContentRequestDto
        {
            TestId = testId,
            ProcessingType = ProcessingType.Flashcards,
            NumberOfCards = numberOfCards,
            DifficultyLevel = difficultyLevel
        };
    }

    public static GenerateContentRequestDto GeneratePracticeTestRequest(
        int testId,
        int? numberOfQuestions = null,
        List<string>? questionTypes = null,
        bool includeAnswerExplanations = true)
    {
        return new GenerateContentRequestDto
        {
            TestId = testId,
            ProcessingType = ProcessingType.PracticeTest,
            NumberOfQuestions = numberOfQuestions,
            QuestionTypes = questionTypes,
            IncludeAnswerExplanations = includeAnswerExplanations
        };
    }

    public static GenerateContentRequestDto GenerateSummaryRequest(
        int testId,
        string? summaryLength = null,
        string? summaryFormat = null)
    {
        return new GenerateContentRequestDto
        {
            TestId = testId,
            ProcessingType = ProcessingType.Summary,
            SummaryLength = summaryLength,
            SummaryFormat = summaryFormat
        };
    }

    #endregion
}
