namespace SScreentify.Core.Platform;

public static class SnippingToolConflict
{
    /// <summary>
    /// Interprets HKCU\Control Panel\Keyboard\PrintScreenKeyForSnippingEnabled.
    /// Only a DWORD 1 is known to mean Windows claims the key; anything else is not treated as a conflict.
    /// </summary>
    public static bool IsPrintScreenTaken(object? registryValue) => registryValue is int value && value == 1;
}
