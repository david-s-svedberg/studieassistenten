namespace StudieAssistenten.Client.Services;

/// <summary>
/// Service to manage test state changes and notify subscribers.
/// This allows components to react to test updates (like name changes) without page reloads.
/// </summary>
public class TestStateService
{
    /// <summary>
    /// Event that fires when a test is updated (e.g., name changed)
    /// </summary>
    public event Action? OnTestChanged;

    /// <summary>
    /// Notify all subscribers that a test has been updated
    /// </summary>
    public void NotifyTestChanged()
    {
        OnTestChanged?.Invoke();
    }
}
