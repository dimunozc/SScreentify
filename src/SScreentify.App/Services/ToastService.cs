using System.Diagnostics;

namespace SScreentify.App.Services;

/// <summary>Notifications. Stub until the toast window exists (Task 13).</summary>
public sealed class ToastService
{
    public void Show(string message, Action? onClick = null, string? buttonText = null, Action? onButton = null, bool isError = false) =>
        Debug.WriteLine($"[toast{(isError ? " error" : "")}] {message}");
}
