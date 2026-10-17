namespace SScreentify.Core.Hotkeys;

/// <summary>Values match Win32 MOD_* flags.</summary>
[Flags]
public enum HotkeyModifiers { None = 0, Alt = 1, Control = 2, Shift = 4, Win = 8 }

public sealed record HotkeySetting(bool Enabled, HotkeyModifiers Modifiers, int VirtualKey);

public enum HotkeyValidationError { None, Invalid, Duplicate }

public static class HotkeyRules
{
    public const int VkSnapshot = 0x2C;

    private static readonly HashSet<int> ModifierKeys = new() { 0x10, 0x11, 0x12, 0x5B, 0x5C, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 };

    private static readonly Dictionary<int, string> KeyNames = new()
    {
        [VkSnapshot] = "Impr Pant", [0x08] = "Retroceso", [0x09] = "Tab", [0x0D] = "Intro", [0x13] = "Pausa",
        [0x20] = "Espacio", [0x21] = "RePág", [0x22] = "AvPág", [0x23] = "Fin", [0x24] = "Inicio",
        [0x25] = "Izquierda", [0x26] = "Arriba", [0x27] = "Derecha", [0x28] = "Abajo",
        [0x2D] = "Insert", [0x2E] = "Supr",
    };

    public static HotkeyValidationError Validate(IReadOnlyList<HotkeySetting> hotkeys)
    {
        var enabled = hotkeys.Where(h => h.Enabled).ToList();
        if (enabled.Any(h => h.VirtualKey == 0 || ModifierKeys.Contains(h.VirtualKey)))
            return HotkeyValidationError.Invalid;
        return enabled.Distinct().Count() != enabled.Count ? HotkeyValidationError.Duplicate : HotkeyValidationError.None;
    }

    public static string Display(HotkeySetting h)
    {
        var parts = new List<string>();
        if (h.Modifiers.HasFlag(HotkeyModifiers.Control)) parts.Add("Ctrl");
        if (h.Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        if (h.Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Mayús");
        if (h.Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
        parts.Add(KeyName(h.VirtualKey));
        return string.Join(" + ", parts);
    }

    private static string KeyName(int vk) => vk switch
    {
        _ when KeyNames.TryGetValue(vk, out var name) => name,
        >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A => ((char)vk).ToString(),
        >= 0x70 and <= 0x87 => $"F{vk - 0x6F}",
        _ => $"Tecla 0x{vk:X2}",
    };
}
