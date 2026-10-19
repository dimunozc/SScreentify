using Microsoft.Win32;

namespace SScreentify.App.Services;

/// <summary>The "start with Windows" entry under a Run key (HKCU\Software\Microsoft\Windows\CurrentVersion\Run in the app).</summary>
public sealed class AutoStart
{
    private const string ValueName = "SScreentify";
    private readonly RegistryKey _runKey;

    public AutoStart(RegistryKey runKeyRoot) => _runKey = runKeyRoot;

    public static AutoStart ForCurrentUser() =>
        new(Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"));

    public void Apply(bool enabled, string exePath)
    {
        if (enabled) _runKey.SetValue(ValueName, $"\"{exePath}\"");
        else _runKey.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    public bool IsEnabled() => _runKey.GetValue(ValueName) is string;
}
