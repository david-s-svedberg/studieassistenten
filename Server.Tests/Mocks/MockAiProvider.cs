using StudieAssistenten.Server.Services.AI.Abstractions;

namespace StudieAssistenten.Server.Tests.Mocks;

/// <summary>
/// Mock implementation of IAiProvider for testing.
/// Returns predefined responses instead of calling real AI APIs.
/// </summary>
public class MockAiProvider : IAiProvider
{
    private int _callCount = 0;

    public int CallCount => _callCount;
    public AiProviderType ProviderType => AiProviderType.Anthropic;

    public bool IsConfigured() => true;

    public Task<AiResponse> SendMessageAsync(AiRequest request, CancellationToken cancellationToken = default)
    {
        _callCount++;

        // Return different responses based on the prompt content
        string responseText;

        var systemPrompt = request.SystemPrompt ?? string.Empty;
        var userPrompt = request.UserPrompt;

        if (systemPrompt.Contains("flashcard", StringComparison.OrdinalIgnoreCase) ||
            userPrompt.Contains("flashcard", StringComparison.OrdinalIgnoreCase))
        {
            responseText = GenerateMockFlashcards();
        }
        else if (systemPrompt.Contains("practice test", StringComparison.OrdinalIgnoreCase) ||
            systemPrompt.Contains("quiz", StringComparison.OrdinalIgnoreCase))
        {
            responseText = GenerateMockPracticeTest();
        }
        else if (systemPrompt.Contains("summary", StringComparison.OrdinalIgnoreCase) ||
            userPrompt.Contains("summary", StringComparison.OrdinalIgnoreCase))
        {
            responseText = GenerateMockSummary();
        }
        else if (systemPrompt.Contains("test name", StringComparison.OrdinalIgnoreCase) ||
            userPrompt.Contains("suggest", StringComparison.OrdinalIgnoreCase))
        {
            responseText = "Introduction to Testing";
        }
        else
        {
            responseText = "Mock AI response for integration testing.";
        }

        // Create a mock AiResponse
        var response = new AiResponse
        {
            Id = $"msg_mock_{Guid.NewGuid():N}",
            Content = responseText,
            Model = "mock-model",
            Provider = AiProviderType.Anthropic,
            StopReason = "end_turn",
            Usage = new AiUsage
            {
                InputTokens = systemPrompt.Split(' ').Length + userPrompt.Split(' ').Length,
                OutputTokens = responseText.Split(' ').Length,
                CacheReadTokens = 0,
                CacheCreationTokens = 0
            }
        };

        return Task.FromResult(response);
    }

    private string GenerateMockFlashcards()
    {
        return @"[
    {
        ""question"": ""What is integration testing?"",
        ""answer"": ""Integration testing is a level of software testing where individual units are combined and tested as a group to expose faults in the interaction between integrated units.""
    },
    {
        ""question"": ""What is the purpose of mocking in tests?"",
        ""answer"": ""Mocking allows you to replace real dependencies with controlled fake implementations, enabling isolated testing and avoiding external API calls.""
    },
    {
        ""question"": ""What is WebApplicationFactory?"",
        ""answer"": ""WebApplicationFactory is a class in ASP.NET Core that bootstraps an application in-memory for integration testing, providing a TestServer and HttpClient.""
    },
    {
        ""question"": ""Why use SQLite in-memory for testing?"",
        ""answer"": ""SQLite in-memory database provides a fast, isolated database for each test that respects constraints and supports SQL queries, unlike EF Core's InMemory provider.""
    },
    {
        ""question"": ""What is the test pyramid?"",
        ""answer"": ""The test pyramid is a testing strategy with many unit tests at the base, fewer integration tests in the middle, and few end-to-end tests at the top.""
    }
]";
    }

    private string GenerateMockPracticeTest()
    {
        return @"# Practice Test: Integration Testing Fundamentals

## Question 1 (Multiple Choice)
Which testing tool is recommended for Blazor component testing?
A) Selenium
B) bUnit
C) Postman
D) JMeter

**Answer:** B) bUnit
**Explanation:** bUnit is purpose-built for testing Blazor components without requiring a browser, making it fast and lightweight.

## Question 2 (True/False)
True or False: EF Core's InMemory database provider enforces unique constraints and foreign keys.

**Answer:** False
**Explanation:** The EF Core InMemory provider does not enforce many constraints, which is why SQLite in-memory is recommended for testing.

## Question 3 (Short Answer)
What is the main advantage of integration tests over unit tests?

**Answer:** Integration tests validate the interaction between multiple components and can catch issues that unit tests miss, such as database constraints, serialization issues, and service integration problems.

## Question 4 (Multiple Choice)
Which NuGet package provides WebApplicationFactory?
A) Microsoft.AspNetCore.Testing
B) Microsoft.AspNetCore.Mvc.Testing
C) Microsoft.Testing.WebApplications
D) xUnit.WebApplicationFactory

**Answer:** B) Microsoft.AspNetCore.Mvc.Testing
**Explanation:** This package contains WebApplicationFactory and related testing utilities for ASP.NET Core applications.

## Question 5 (Short Answer)
Why should external API calls be mocked in integration tests?

**Answer:** Mocking external APIs makes tests faster, more reliable, deterministic, and avoids incurring costs or rate limits from real API services.";
    }

    private string GenerateMockSummary()
    {
        return @"# Summary: Integration Testing in ASP.NET Core

## Overview
Integration testing validates that multiple components of an application work together correctly. Unlike unit tests that test components in isolation, integration tests verify the interactions between services, databases, and APIs.

## Key Concepts

### WebApplicationFactory
WebApplicationFactory is the foundation of ASP.NET Core integration testing. It:
- Bootstraps the application in-memory
- Provides a TestServer for hosting
- Creates HttpClient instances for making requests
- Allows service configuration customization

### Database Testing
For reliable integration tests, use SQLite in-memory databases instead of EF Core's InMemory provider:
- **SQLite in-memory**: Enforces constraints, supports SQL, closer to production
- **EF InMemory**: Lacks constraint enforcement, not recommended by EF Core team

### Mocking External Services
Mock external dependencies to:
- Avoid real API calls and costs
- Make tests faster and more reliable
- Control test scenarios and responses
- Prevent network-related test failures

## Best Practices

1. **Test Isolation**: Each test should have its own database state
2. **Arrange-Act-Assert**: Follow clear test structure
3. **Test Real Scenarios**: Focus on actual user workflows
4. **Mock Wisely**: Mock external services but test real business logic
5. **Fast Execution**: Keep integration tests fast (< 1 second each)

## Testing Pyramid
- **Unit Tests (60%)**: Fast, isolated, many
- **Integration Tests (30%)**: Moderate speed, validate interactions
- **End-to-End Tests (10%)**: Slow, validate complete workflows

Integration tests provide the best balance of coverage, speed, and reliability, catching the majority of bugs while remaining maintainable.";
    }

    public void Reset()
    {
        _callCount = 0;
    }
}
