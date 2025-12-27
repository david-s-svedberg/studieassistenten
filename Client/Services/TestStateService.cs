namespace StudieAssistenten.Client.Services;

/// <summary>
/// Service to manage test state changes and notify subscribers.
/// This allows components to react to test updates (like name changes) without page reloads.
/// </summary>
public class TestStateService
{
    private static int _notifyCallCount = 0;

    /// <summary>
    /// Event that fires when a test is updated (e.g., name changed)
    /// </summary>
    public event Action? OnTestChanged;

    /// <summary>
    /// Notify all subscribers that a test has been updated
    /// </summary>
    public void NotifyTestChanged()
    {
        _notifyCallCount++;
        var subscriberCount = OnTestChanged?.GetInvocationList().Length ?? 0;
        Console.WriteLine($"[TestStateService] NotifyTestChanged called (call #{_notifyCallCount}). Subscribers: {subscriberCount}");
        OnTestChanged?.Invoke();
    }

    public int GetSubscriberCount() => OnTestChanged?.GetInvocationList().Length ?? 0;
}
