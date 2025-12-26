using StudieAssistenten.Server.Services;

namespace StudieAssistenten.Server.Tests.Mocks;

/// <summary>
/// Mock implementation of IOcrService for testing.
/// Returns predefined OCR text instead of calling Azure Computer Vision or Tesseract.
/// </summary>
public class MockOcrService : IOcrService
{
    private int _callCount = 0;
    private readonly Dictionary<string, string> _mockResponses = new();

    public int CallCount => _callCount;

    public MockOcrService()
    {
        // Set up default mock responses for common test scenarios
        SetDefaultMockResponses();
    }

    public Task<string> ExtractTextFromImageAsync(string imagePath, string language = "swe")
    {
        _callCount++;

        // If we have a specific mock response for this file, return it
        if (_mockResponses.ContainsKey(imagePath))
        {
            return Task.FromResult(_mockResponses[imagePath]);
        }

        // Otherwise return a default OCR response
        return Task.FromResult(GetDefaultOcrText(imagePath));
    }

    public Task<bool> IsAvailableAsync()
    {
        // Mock OCR is always available
        return Task.FromResult(true);
    }

    /// <summary>
    /// Allows tests to configure custom OCR responses for specific files
    /// </summary>
    public void SetMockResponse(string filePath, string extractedText)
    {
        _mockResponses[filePath] = extractedText;
    }

    /// <summary>
    /// Resets the mock to its initial state
    /// </summary>
    public void Reset()
    {
        _callCount = 0;
        _mockResponses.Clear();
        SetDefaultMockResponses();
    }

    private void SetDefaultMockResponses()
    {
        // Sample PDF/image content for testing
        _mockResponses["sample.pdf"] = @"
Integration Testing Best Practices

1. Test Isolation
Each test should run independently without affecting other tests.
Use separate database instances or transactions.

2. Arrange-Act-Assert Pattern
Arrange: Set up test data and dependencies
Act: Execute the code being tested
Assert: Verify the expected outcome

3. Mock External Dependencies
Replace real external services with mock implementations to:
- Avoid network calls
- Control test scenarios
- Improve test speed and reliability

4. Use In-Memory Databases
SQLite in-memory databases provide:
- Fast test execution
- Constraint enforcement
- SQL support
- Isolation between tests

5. Keep Tests Fast
Integration tests should complete in under 1 second.
Slow tests reduce developer productivity and CI/CD efficiency.
";

        _mockResponses["document1.pdf"] = @"
Chapter 1: Introduction to Software Testing

Software testing is the process of evaluating and verifying that a software application
or product does what it is supposed to do. The benefits of testing include preventing bugs,
reducing development costs, and improving performance.

Types of Testing:
- Unit Testing: Testing individual components in isolation
- Integration Testing: Testing combined parts of an application
- End-to-End Testing: Testing complete user workflows
- Performance Testing: Testing system performance under load
- Security Testing: Testing for vulnerabilities

Testing Pyramid:
The testing pyramid is a concept that groups software tests into three categories:
unit tests (base), integration tests (middle), and end-to-end tests (top).
The pyramid shape indicates that you should have more unit tests than integration tests,
and more integration tests than end-to-end tests.
";

        _mockResponses["document2.pdf"] = @"
Advanced Testing Techniques

Test-Driven Development (TDD)
TDD is a software development approach where tests are written before the code.
The cycle is: Red (write failing test) → Green (make it pass) → Refactor (improve code).

Behavior-Driven Development (BDD)
BDD extends TDD by writing test cases in natural language that even non-programmers
can read. Tools like SpecFlow and Cucumber enable BDD.

Mocking and Stubbing
- Mocks: Objects that verify method calls and interactions
- Stubs: Objects that return predefined values
- Fakes: Working implementations with simplified logic

Code Coverage
Code coverage measures what percentage of code is executed during tests.
While 100% coverage is rarely practical, aim for 70-80% coverage of critical paths.
Remember: High coverage doesn't guarantee good tests!
";
    }

    private string GetDefaultOcrText(string identifier)
    {
        return $@"Sample OCR extracted text for {identifier}

This is mock OCR content returned during integration testing.
It simulates text extraction from PDF or image files.

Key Points:
- Integration tests validate component interactions
- Mock services replace external dependencies
- SQLite in-memory databases provide fast, isolated testing
- WebApplicationFactory enables in-memory application hosting

The mock OCR service returns this predefined text instead of
performing actual optical character recognition, making tests
faster and more reliable.

Test execution timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
";
    }
}
