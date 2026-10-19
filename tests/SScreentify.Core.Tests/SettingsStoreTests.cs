using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;
using SScreentify.Core.Hotkeys;
using SScreentify.Core.Settings;

namespace SScreentify.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "SScreentify.Tests", Guid.NewGuid().ToString("N"));
    private string SettingsPath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var result = new SettingsStore(SettingsPath).Load();
        Assert.False(result.WasCorrupt);
        Assert.Equal(new AppSettings(), result.Settings);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var store = new SettingsStore(SettingsPath);
        var settings = new AppSettings
        {
            ShowNotifications = false,
            CaptureCursor = true,
            KeepSelection = true,
            StartWithWindows = true,
            MainHotkey = new HotkeySetting(true, HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x41),
            SaveFullscreenHotkey = new HotkeySetting(true, HotkeyModifiers.Alt, HotkeyRules.VkSnapshot),
            Format = ImageFormat.Jpeg,
            JpegQuality = 75,
            DefaultFolder = @"D:\Capturas",
            LastSaveFolder = @"D:\Otra",
            Color = "#22C55E",
            Thickness = 7,
            FontSize = 32,
            CensorStyle = CensorStyle.Solid,
            LastSelection = new PixelRect(-100, 20, 300, 200),
        };
        store.Save(settings);

        Assert.Equal(new SettingsLoadResult(settings, false), store.Load());
        Assert.Contains("\"Jpeg\"", File.ReadAllText(SettingsPath));
    }

    [Fact]
    public void Load_CorruptFile_BacksUpAndReturnsDefaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, "{nope");

        var result = new SettingsStore(SettingsPath).Load();

        Assert.True(result.WasCorrupt);
        Assert.Equal(new AppSettings(), result.Settings);
        Assert.Equal("{nope", File.ReadAllText(SettingsPath + ".bak"));
        Assert.False(File.Exists(SettingsPath));
    }

    [Fact]
    public void Load_ClampsOutOfRangeValues()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, """{ "JpegQuality": 500, "Thickness": 0, "FontSize": 1000 }""");

        var settings = new SettingsStore(SettingsPath).Load().Settings;

        Assert.Equal(100, settings.JpegQuality);
        Assert.Equal(1, settings.Thickness);
        Assert.Equal(96, settings.FontSize);
        Assert.True(settings.ShowNotifications);
    }

    [Fact]
    public void Load_InvalidColor_FallsBackToDefault()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, """{ "Color": "rojo" }""");
        Assert.Equal("#F43F5E", new SettingsStore(SettingsPath).Load().Settings.Color);
    }

    [Fact]
    public void Save_LeavesNoTempFile()
    {
        new SettingsStore(SettingsPath).Save(new AppSettings());
        Assert.Equal(new[] { SettingsPath }, Directory.GetFiles(_dir));
    }
}
