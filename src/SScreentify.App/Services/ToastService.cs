using SScreentify.App.Ui;

namespace SScreentify.App.Services;

/// <summary>Shows one notification at a time; a new one replaces the visible one.
/// Callers decide whether a success notice respects ShowNotifications; errors always show.</summary>
public interface IToastSink
{
    void Show(string message, Action? onClick = null, string? buttonText = null, Action? onButton = null, bool isError = false);
}

public sealed class ToastService : IToastSink
{
    private ToastWindow? _current;

    public void Show(string message, Action? onClick = null, string? buttonText = null, Action? onButton = null, bool isError = false)
    {
        _current?.Close();
        var toast = new ToastWindow(message, onClick, buttonText, onButton, isError);
        toast.Closed += (_, _) => { if (_current == toast) _current = null; };
        _current = toast;
        toast.Show();
    }
}
