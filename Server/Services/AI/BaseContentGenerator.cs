using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services.AI;

/// <summary>
/// Base class for AI content generators.
/// Provides common functionality for rate limiting and document retrieval.
/// </summary>
public abstract class BaseContentGenerator
{
    protected readonly ApplicationDbContext Context;
    protected readonly IRateLimitingService RateLimitingService;
    protected readonly IConfiguration Configuration;
    protected readonly ILogger Logger;

    protected BaseContentGenerator(
        ApplicationDbContext context,
        IRateLimitingService rateLimitingService,
        IConfiguration configuration,
        ILogger logger)
    {
        Context = context;
        RateLimitingService = rateLimitingService;
        Configuration = configuration;
        Logger = logger;
    }

    /// <summary>
    /// Checks if the request can be made within rate limits.
    /// Throws InvalidOperationException if limit is exceeded.
    /// </summary>
    protected async Task CheckRateLimitAsync()
    {
        if (!Configuration.GetValue<bool>("RateLimiting:EnableRateLimiting", true))
        {
            return;
        }

        var canMakeRequest = await RateLimitingService.CanMakeRequestAsync();
        if (!canMakeRequest)
        {
            var usage = await RateLimitingService.GetTodayUsageAsync();
            var limit = RateLimitingService.GetDailyTokenLimit();
            throw new InvalidOperationException(
                $"Daily token limit exceeded. Used: {usage.TotalTokens:N0}/{limit:N0} tokens. Please try again tomorrow.");
        }
    }

    /// <summary>
    /// Retrieves a document with extracted text.
    /// Throws InvalidOperationException if document not found or has no text.
    /// </summary>
    protected async Task<StudyDocument> GetDocumentWithTextAsync(int documentId)
    {
        var document = await Context.StudyDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
        {
            throw new InvalidOperationException($"Document {documentId} not found");
        }

        if (string.IsNullOrWhiteSpace(document.ExtractedText))
        {
            throw new InvalidOperationException($"Document {documentId} has no extracted text. Please run OCR first.");
        }

        return document;
    }
}
