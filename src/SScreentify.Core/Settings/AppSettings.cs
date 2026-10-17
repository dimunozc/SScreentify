using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;
using SScreentify.Core.Hotkeys;

namespace SScreentify.Core.Settings;

public enum ImageFormat { Png, Jpeg, Bmp }

public sealed record AppSettings
{
    public bool ShowNotifications { get; init; } = true;
    public bool CaptureCursor { get; init; }
    public bool KeepSelection { get; init; }
    public bool StartWithWindows { get; init; }
    public HotkeySetting MainHotkey { get; init; } = new(true, HotkeyModifiers.None, HotkeyRules.VkSnapshot);
    public HotkeySetting SaveFullscreenHotkey { get; init; } = new(false, HotkeyModifiers.Shift, HotkeyRules.VkSnapshot);
    public ImageFormat Format { get; init; } = ImageFormat.Png;
    public int JpegQuality { get; init; } = 90;
    /// <summary>null means Pictures\sScreentify.</summary>
    public string? DefaultFolder { get; init; }
    public string? LastSaveFolder { get; init; }
    public string Color { get; init; } = "#F43F5E";
    public double Thickness { get; init; } = 4;
    public double FontSize { get; init; } = 20;
    public CensorStyle CensorStyle { get; init; } = CensorStyle.Pixelate;
    public PixelRect? LastSelection { get; init; }

    /// <summary>Brings values read from disk back into their valid ranges.</summary>
    public AppSettings Normalize()
    {
        var defaults = new AppSettings();
        return this with
        {
            JpegQuality = Math.Clamp(JpegQuality, 1, 100),
            Thickness = Math.Clamp(Thickness, 1, 40),
            FontSize = Math.Clamp(FontSize, 8, 96),
            Color = IsColor(Color) ? Color : defaults.Color,
            MainHotkey = MainHotkey ?? defaults.MainHotkey,
            SaveFullscreenHotkey = SaveFullscreenHotkey ?? defaults.SaveFullscreenHotkey,
        };
    }

    private static bool IsColor(string? value)
    {
        if (value is null) return false;
        try { Rgba.Parse(value); return true; }
        catch (FormatException) { return false; }
    }
}
