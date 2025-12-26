using StudieAssistenten.Client.Shared;

namespace StudieAssistenten.Client.Services;

public interface IToastService
{
    event Action<string, string, Toast.ToastType>? OnShow;
    void ShowSuccess(string message, string title = "Success");
    void ShowError(string message, string title = "Error");
    void ShowWarning(string message, string title = "Warning");
    void ShowInfo(string message, string title = "Info");
}

public class ToastService : IToastService
{
    public event Action<string, string, Toast.ToastType>? OnShow;

    public void ShowSuccess(string message, string title = "Success")
    {
        OnShow?.Invoke(message, title, Toast.ToastType.Success);
    }

    public void ShowError(string message, string title = "Error")
    {
        OnShow?.Invoke(message, title, Toast.ToastType.Error);
    }

    public void ShowWarning(string message, string title = "Warning")
    {
        OnShow?.Invoke(message, title, Toast.ToastType.Warning);
    }

    public void ShowInfo(string message, string title = "Info")
    {
        OnShow?.Invoke(message, title, Toast.ToastType.Info);
    }
}
