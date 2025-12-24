using Microsoft.EntityFrameworkCore;
using StudieAssistenten.Server.Data;
using StudieAssistenten.Shared.Models;

namespace StudieAssistenten.Server.Services;

public interface IRateLimitingService
{
    /// <summary>
    /// Check if we're within the daily token limit
    /// </summary>
    Task<bool> CanMakeRequestAsync();

    /// <summary>
    /// Get current usage for today
    /// </summary>
    Task<UsageTracking> GetTodayUsageAsync();

    /// <summary>
    /// Record token usage after making an API call
    /// </summary>
    Task RecordUsageAsync(long inputTokens, long outputTokens);

    /// <summary>
    /// Get the configured daily token limit
    /// </summary>
    long GetDailyTokenLimit();
}

public class RateLimitingService : IRateLimitingService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RateLimitingService> _logger;

    public RateLimitingService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<RateLimitingService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public long GetDailyTokenLimit()
    {
        // Default to 1 million tokens per day if not configured
        return _configuration.GetValue<long>("RateLimiting:DailyTokenLimit", 1_000_000);
    }

    public async Task<bool> CanMakeRequestAsync()
    {
        var usage = await GetTodayUsageAsync();
        var limit = GetDailyTokenLimit();

        var canMakeRequest = usage.TotalTokens < limit;

        if (!canMakeRequest)
        {
            _logger.LogWarning(
                "Daily token limit reached. Used: {UsedTokens}/{Limit} tokens",
                usage.TotalTokens,
                limit);
        }

        return canMakeRequest;
    }

    public async Task<UsageTracking> GetTodayUsageAsync()
    {
        var today = DateTime.UtcNow.Date;

        var usage = await _context.UsageTrackings
            .FirstOrDefaultAsync(u => u.Date == today);

        if (usage == null)
        {
            usage = new UsageTracking
            {
                Date = today,
                InputTokens = 0,
                OutputTokens = 0,
                ApiCallCount = 0,
                LastUpdated = DateTime.UtcNow
            };

            _context.UsageTrackings.Add(usage);
            await _context.SaveChangesAsync();
        }

        return usage;
    }

    public async Task RecordUsageAsync(long inputTokens, long outputTokens)
    {
        var usage = await GetTodayUsageAsync();

        usage.InputTokens += inputTokens;
        usage.OutputTokens += outputTokens;
        usage.ApiCallCount++;
        usage.LastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "API usage recorded: +{InputTokens} input, +{OutputTokens} output. Daily total: {TotalTokens} tokens ({CallCount} calls)",
            inputTokens,
            outputTokens,
            usage.TotalTokens,
            usage.ApiCallCount);
    }
}
