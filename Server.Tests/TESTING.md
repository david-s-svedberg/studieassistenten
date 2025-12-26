# StudieAssistenten Testing Strategy

**Status:** Infrastructure Complete - Implementation In Progress
**Date:** 2025-12-26
**Approach:** Multi-layer testing pyramid with emphasis on integration tests

---

## Overview

This document describes the comprehensive testing strategy for the StudieAssistenten application. The approach prioritizes integration tests for high coverage and reliability while minimizing maintenance overhead.

## Testing Pyramid

```
        /\
       /E2E\          10% - Critical user workflows only
      /------\
     /  bUnit \        30% - Component-level UI logic
    /----------\
   /Integration \      60% - API + Database + Business Logic
  /--------------\
```

### Layer Breakdown

| Layer | Tool | Purpose | Coverage Target | Status |
|-------|------|---------|----------------|--------|
| **Integration** | xUnit + WebApplicationFactory | API endpoints with real DB, mocked external services | 60% | ✅ Complete (44 tests) |
| **Component** | bUnit | Blazor component testing | 30% | 🚧 In Progress (32 tests) |
| **End-to-End** | Playwright | Critical workflows in browser | 10% | 📋 Planned |

---

## Phase 1: Integration Testing Infrastructure ✅ COMPLETE

### What's Implemented

#### 1. Test Fixtures (`Fixtures/`)

##### `TestWebApplicationFactory.cs`
- **Purpose:** Bootstraps application in-memory for testing
- **Features:**
  - SQLite in-memory database (connection stays open for test lifetime)
  - Mocks external services (Anthropic API, OCR)
  - Configures test environment
  - Provides HttpClient for making requests

**Usage:**
```csharp
public class MyControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MyControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }
}
```

##### `DatabaseFixture.cs`
- **Purpose:** Database management utilities for tests
- **Features:**
  - Create test users
  - Create tests with documents
  - Create generated content
  - Clear database between tests

**Usage:**
```csharp
var dbFixture = new DatabaseFixture(_factory.Services);
var user = await dbFixture.CreateTestUser("test@example.com");
var test = await dbFixture.CreateTestWithDocuments(user.Id, documentCount: 2);
```

##### `AuthenticationFixture.cs`
- **Purpose:** Authentication helpers for testing protected endpoints
- **Features:**
  - Create authenticated HTTP clients
  - Create test principals
  - TestAuthHandler for bypassing real OAuth

**Usage:**
```csharp
var client = AuthenticationFixture.CreateAuthenticatedClient(_factory, testUser);
var response = await client.GetAsync("/api/tests"); // Authenticated request
```

#### 2. Mock Services (`Mocks/`)

##### `MockAnthropicClient.cs`
- **Replaces:** Real Anthropic API calls
- **Returns:** Predefined flashcards, practice tests, summaries
- **Benefits:** No API costs, deterministic results, fast execution

**Features:**
```csharp
// Returns different responses based on prompt content
- Flashcards: 5 sample cards about testing
- Practice Tests: 5 sample questions with answers
- Summaries: Structured summary about integration testing
- Test Names: "Introduction to Testing"
```

**Tracking:**
```csharp
var mockClient = (MockAnthropicClient)serviceProvider.GetRequiredService<IAnthropicApiClient>();
Assert.Equal(1, mockClient.CallCount); // Verify API was called
mockClient.Reset(); // Reset for next test
```

##### `MockOcrService.cs`
- **Replaces:** Azure Computer Vision / Tesseract OCR
- **Returns:** Predefined extracted text for test files
- **Benefits:** No OCR processing time, consistent results

**Features:**
```csharp
// Pre-configured responses for common test files
SetMockResponse("document1.pdf", "Custom extracted text");

// Language parameter support
ExtractTextFromImageAsync(path, language: "swe");

// Always available
IsAvailableAsync() => true
```

#### 3. Test Data Builders (`TestData/`)

##### `TestDataBuilder.cs`
Fluent API for creating test data with readable, maintainable tests.

**User Builder:**
```csharp
var user = await new TestDataBuilder(context)
    .User()
    .WithEmail("user@example.com")
    .WithName("Test User")
    .WithProfilePicture("https://example.com/avatar.jpg")
    .BuildAsync();
```

**Test Builder:**
```csharp
var test = await new TestDataBuilder(context)
    .Test(userId)
    .WithName("My Test")
    .WithDescription("Test description")
    .WithInstructions("Test instructions")
    .WithDocument("doc1.pdf", "Sample content")
    .WithDocument("doc2.pdf", "More content")
    .BuildAsync();
```

**DTO Builders:**
```csharp
// Create Test Request
var request = TestDataBuilder.CreateTestRequest(
    name: "New Test",
    description: "Description"
);

// Generate Flashcards Request
var request = TestDataBuilder.GenerateFlashcardsRequest(
    testId: 1,
    numberOfCards: 15,
    difficultyLevel: "Advanced"
);

// Generate Practice Test Request
var request = TestDataBuilder.GeneratePracticeTestRequest(
    testId: 1,
    numberOfQuestions: 10,
    questionTypes: new List<string> { "MultipleChoice", "TrueFalse" },
    includeAnswerExplanations: true
);

// Generate Summary Request
var request = TestDataBuilder.GenerateSummaryRequest(
    testId: 1,
    summaryLength: "Detailed",
    summaryFormat: "Outline"
);
```

#### 4. Integration Tests (`Integration/Controllers/`)

##### `TestsControllerTests.cs` ✅
**Coverage:** Full CRUD operations for tests
- ✅ GET /api/tests - List all user tests
- ✅ GET /api/tests/{id} - Get specific test
- ✅ POST /api/tests - Create new test
- ✅ PUT /api/tests/{id} - Update test
- ✅ DELETE /api/tests/{id} - Delete test

**Security Tests:**
- ✅ Other users cannot access/modify your tests (403 Forbidden)
- ✅ Non-existent tests return 404 Not Found
- ✅ Invalid requests return 400 Bad Request

**Sample Test:**
```csharp
[Fact]
public async Task GetTest_WithValidId_ReturnsTest()
{
    // Arrange
    var builder = new TestDataBuilder(_dbFixture.Context);
    var test = await builder.Test(_testUser.Id)
        .WithName("Integration Test")
        .WithDocument("document1.pdf")
        .BuildAsync();

    // Act
    var response = await _client.GetAsync($"/api/tests/{test.Id}");

    // Assert
    response.Should().BeSuccessful();
    var testDto = await response.Content.ReadFromJsonAsync<TestDto>();
    testDto!.Id.Should().Be(test.Id);
    testDto.DocumentCount.Should().Be(1);
}
```

##### `ContentGenerationControllerTests.cs` ✅
**Coverage:** AI content generation with mocked Anthropic API
- ✅ POST /api/ContentGeneration/generate - Generate flashcards
- ✅ POST /api/ContentGeneration/generate - Generate practice test
- ✅ POST /api/ContentGeneration/generate - Generate summary
- ✅ GET /api/ContentGeneration/test/{testId} - List generated content
- ✅ DELETE /api/ContentGeneration/{id} - Delete generated content

**Options Testing:**
- ✅ Flashcard options (numberOfCards, difficultyLevel)
- ✅ Practice test options (numberOfQuestions, questionTypes, includeExplanations)
- ✅ Summary options (summaryLength, summaryFormat)

**Security Tests:**
- ✅ Cannot generate content for other users' tests
- ✅ Cannot delete other users' generated content
- ✅ Returns 400 Bad Request if test has no documents

---

## Phase 2: Component Testing with bUnit 🚧 IN PROGRESS

### What's Implemented

#### 1. Client.Tests Project
- **Created:** Client.Tests xUnit project
- **Dependencies:** bUnit 1.31.3, FluentAssertions 8.8.0, Moq 4.20.72
- **References:** Client project, Shared project

#### 2. Component Tests (`Components/`)

##### `FlashcardOptionsDialogTests.cs` ✅ Complete (10 tests)
**Coverage:** Full coverage of FlashcardOptionsDialog component
- Modal rendering and UI elements
- Default values (AI decides, Mixed difficulty)
- Dropdown options (numberOfCards, difficultyLevel)
- User interactions and event callbacks
- Cancel and close functionality

**Key Tests:**
- Verify all 7 number of cards options (AI decides, 5, 10, 15, 20, 25, 30)
- Verify all 4 difficulty levels (Mixed, Basic, Intermediate, Advanced)
- Test EventCallback invocations with correct parameter values
- Validate re-rendering behavior after user selections

##### `PracticeTestOptionsDialogTests.cs` ✅ Complete (12 tests)
**Coverage:** Full coverage of PracticeTestOptionsDialog component
- Modal rendering with validation logic
- Default values (Mixed checked, explanations enabled)
- Checkbox interactions (5 question types)
- Validation: button disabled when no question type selected
- Multiple simultaneous selections
- Custom option combinations

**Key Tests:**
- Validate button disabled state based on IsValid property
- Test all 5 question type checkboxes
- Verify validation error message display
- Test combinations of question types
- Validate explanations checkbox toggle

##### `SummaryOptionsDialogTests.cs` ✅ Complete (10 tests)
**Coverage:** Full coverage of SummaryOptionsDialog component
- Modal rendering and UI elements
- Default values (Standard length, Bullets format)
- Dropdown options (length, format)
- User interactions and event callbacks
- Cancel and close functionality

**Key Tests:**
- Verify all 3 length options (Brief, Standard, Detailed)
- Verify all 3 format options (Bullets, Paragraphs, Outline)
- Test EventCallback invocations with correct parameter values
- Validate combined selection changes

#### 3. Testing Patterns Established
- **Component Isolation:** Each component tested in isolation using bUnit's TestContext
- **Event Testing:** EventCallback parameters validated using captured values
- **Re-Render Handling:** Proper re-querying of elements after state changes
- **FluentAssertions:** Readable, expressive assertions throughout
- **Fast Execution:** All 32 tests run in ~60ms

---

## Project Structure

```
Server.Tests/
├── TESTING.md                                  (this file)
├── StudieAssistenten.Server.Tests.csproj       ✅ Updated with packages
├── Fixtures/
│   ├── TestWebApplicationFactory.cs            ✅ Complete
│   ├── DatabaseFixture.cs                      ✅ Complete
│   └── AuthenticationFixture.cs                ✅ Complete
├── Mocks/
│   ├── MockAnthropicClient.cs                  ✅ Complete
│   └── MockOcrService.cs                       ✅ Complete
├── TestData/
│   └── TestDataBuilder.cs                      ✅ Complete
└── Integration/
    └── Controllers/
        ├── TestsControllerTests.cs             ✅ Complete (16 tests)
        ├── ContentGenerationControllerTests.cs ✅ Complete (12 tests)
        ├── DocumentsControllerTests.cs         ✅ Complete (12 tests)
        └── AuthControllerTests.cs              ✅ Complete (4 tests)

Client.Tests/
├── Client.Tests.csproj                         ✅ Created with bUnit
└── Components/
    ├── FlashcardOptionsDialogTests.cs          ✅ Complete (10 tests)
    ├── PracticeTestOptionsDialogTests.cs       ✅ Complete (12 tests)
    └── SummaryOptionsDialogTests.cs            ✅ Complete (10 tests)
```

---

## Dependencies Added

### NuGet Packages
```xml
<PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="xunit" Version="2.5.3" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="FluentAssertions" Version="8.8.0" />
```

### Server Changes
- **Program.cs:** Added `public partial class Program { }` at end to make it accessible to tests

---

## Key Design Decisions

### 1. SQLite In-Memory vs EF Core InMemory
**Decision:** Use SQLite in-memory
**Rationale:**
- ✅ Enforces foreign keys and unique constraints
- ✅ Supports raw SQL queries
- ✅ Closer to production SQLite behavior
- ❌ EF Core InMemory provider is [discouraged by EF Core team](https://github.com/dotnet/AspNetCore.Docs/issues/24811)

### 2. Mock External Services
**Decision:** Mock Anthropic API and OCR services
**Rationale:**
- ✅ No API costs during testing
- ✅ Deterministic, predictable results
- ✅ Fast test execution (no network calls)
- ✅ Can test edge cases and error scenarios
- ✅ Tests don't fail due to external service issues

### 3. Test Authentication
**Decision:** Use TestAuthHandler instead of real OAuth
**Rationale:**
- ✅ Faster test execution (no OAuth flow)
- ✅ Deterministic user IDs for test isolation
- ✅ No Google OAuth credentials needed for tests
- ✅ Easy to test different user scenarios

### 4. Test Isolation
**Decision:** Clear database between tests using `IAsyncLifetime`
**Rationale:**
- ✅ Each test starts with clean state
- ✅ Tests don't interfere with each other
- ✅ Can run tests in parallel
- ✅ Predictable test results

---

## Known Issues & TODOs

### 🐛 Compilation Errors (Minor - Need Fixing)

1. **DTO Property Mismatches**
   - Issue: Test code references properties that don't exist on DTOs
   - Example: `GeneratedContentDto.ContentType` may be named differently
   - Fix: Check actual DTO properties and update test code

2. **Missing Using Statements**
   - Issue: `ProcessingType` not in scope in some test files
   - Fix: Add `using StudieAssistenten.Shared.Enums;`

3. **FluentAssertions Extension Methods**
   - Issue: `response.Content.Should().BeSuccessful()` doesn't exist
   - Fix: Should be `response.Should().BeSuccessful()` for HttpResponseMessage

4. **Database Context Property Names**
   - Issue: Code uses `_context.Documents` but actual property is `StudyDocuments`
   - Fix: Update to `_context.StudyDocuments` (partially done)

### 📋 Pending Test Files

1. **DocumentsControllerTests.cs**
   - Test file upload
   - Test document retrieval
   - Test document deletion
   - Test OCR processing status

2. **AuthControllerTests.cs**
   - Test login flow
   - Test logout
   - Test user info endpoint
   - Test email whitelist validation

---

## Running Tests

### Run All Tests
```bash
cd StudieAssistenten/Server.Tests
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~TestsControllerTests"
```

### Run Single Test
```bash
dotnet test --filter "FullyQualifiedName~TestsControllerTests.GetAllTests_WhenNoTests_ReturnsEmptyList"
```

### Run with Verbose Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Generate Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Next Steps (Priority Order)

### Phase 1: Integration Tests ✅ COMPLETE
1. ✅ **Fixed compilation errors** in existing test files
2. ✅ **Ran and verified** TestsControllerTests and ContentGenerationControllerTests
3. ✅ **Wrote DocumentsControllerTests** (file upload, retrieval, deletion)
4. ✅ **Wrote AuthControllerTests** (authentication flows)

**Success Criteria:**
- ✅ All integration tests build successfully (44/44 tests)
- ✅ All integration tests pass (100%)
- ✅ Comprehensive coverage of controllers and services

### Phase 2: Component Testing with bUnit 🚧 IN PROGRESS
1. ✅ **Created Client.Tests project**
2. ✅ **Installed bUnit** and configured (v1.31.3)
3. ✅ **Wrote component tests** for all three option dialogs (32 tests)
   - FlashcardOptionsDialog: 10 tests
   - PracticeTestOptionsDialog: 12 tests
   - SummaryOptionsDialog: 10 tests
4. 📋 **Write component tests** for pages (TestDetail, Tests index)
5. 📋 **Write tests for services** (TestService, ContentGenerationService)

**Success Criteria:**
- ✅ 20-30 component tests written (32 tests, exceeding target!)
- ✅ All interactive UI dialog components tested
- 📋 Service layer tested in isolation (pending)

### Phase 3: End-to-End Testing with Playwright (2-3 days) 📋
1. **Create E2E.Tests project**
2. **Install Playwright** and configure
3. **Add data-testid attributes** to key UI elements
4. **Write critical workflow tests**:
   - User login → Create test → Upload document → Generate flashcards
   - Mobile navigation testing
   - Error scenarios
5. **Configure CI/CD** to run E2E tests

**Success Criteria:**
- ✅ 5-10 critical user workflows tested
- ✅ Tests run in headless browser
- ✅ Tests integrated into CI/CD pipeline

### Phase 4: CI/CD Integration (1 day) 📋
1. **Create GitHub Actions workflow** (or Azure Pipelines)
2. **Configure test execution** on every push/PR
3. **Add code coverage reporting**
4. **Add test result badges** to README

**Success Criteria:**
- ✅ Tests run automatically on every commit
- ✅ PR cannot merge if tests fail
- ✅ Code coverage visible in PRs

---

## Testing Best Practices

### DO ✅
- Use test data builders for readable tests
- Follow Arrange-Act-Assert pattern
- Test both happy paths and error scenarios
- Test authorization (ensure users can't access others' data)
- Clear database between tests
- Use meaningful test names that describe what's being tested
- Keep tests fast (< 1 second each)

### DON'T ❌
- Don't call real external APIs in tests
- Don't share state between tests
- Don't use hardcoded IDs (generate them dynamically)
- Don't test implementation details
- Don't skip cleanup (use IAsyncLifetime)
- Don't write tests that depend on execution order

---

## Example: Adding a New Integration Test

```csharp
public class MyNewControllerTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private HttpClient _client;
    private ApplicationUser _testUser = null!;
    private DatabaseFixture _dbFixture = null!;

    public MyNewControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Setup: Create database fixture and test user
        _dbFixture = new DatabaseFixture(_factory.Services);
        _testUser = await _dbFixture.CreateTestUser();

        // Create authenticated client
        _client.Dispose();
        _client = AuthenticationFixture.CreateAuthenticatedClient(_factory, _testUser);
    }

    public async Task DisposeAsync()
    {
        // Cleanup: Clear database and dispose resources
        await _dbFixture.ClearDatabase();
        _dbFixture.Dispose();
        _client.Dispose();
    }

    [Fact]
    public async Task MyTest_WithValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var builder = new TestDataBuilder(_dbFixture.Context);
        var test = await builder.Test(_testUser.Id)
            .WithName("Test Name")
            .BuildAsync();

        var request = TestDataBuilder.CreateTestRequest("New Test");

        // Act
        var response = await _client.PostAsJsonAsync("/api/endpoint", request);

        // Assert
        response.Should().BeSuccessful();
        var result = await response.Content.ReadFromJsonAsync<ResultDto>();
        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
    }
}
```

---

## Resources

- [ASP.NET Core Integration Tests Documentation](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [bUnit Documentation](https://bunit.dev/)
- [Playwright for .NET](https://playwright.dev/dotnet/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [xUnit Documentation](https://xunit.net/)

---

## Questions?

For questions or issues with the testing infrastructure:
1. Check this document first
2. Review example tests in `Integration/Controllers/`
3. Check the test fixtures in `Fixtures/` for usage examples
4. Consult the ASP.NET Core documentation for WebApplicationFactory patterns

---

**Last Updated:** 2025-12-26
**Status:** Phase 2 (Component Testing) IN PROGRESS - 76 total tests passing (100%)
- Integration Tests: 44/44 passing ✅
- Component Tests: 32/32 passing ✅
**Next Milestone:** Phase 2 - Complete page and service tests, then Phase 3 - E2E with Playwright
