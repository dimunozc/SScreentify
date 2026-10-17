using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using SScreentify.App.Export;
using SScreentify.App.Overlay;
using SScreentify.App.Services;
using SScreentify.App.Ui;
using SScreentify.Core.Hotkeys;
using SScreentify.Core.Platform;
using SScreentify.Core.Settings;

namespace SScreentify.App;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private SettingsStore _store = null!;
    private TrayIcon? _tray;
    private HotkeyService? _hotkeys;
    private OverlayController? _overlay;
    private ExportActions? _export;
    private readonly ToastService _toasts = new();

    internal AppSettings Settings { get; private set; } = new();

    /// <summary>Problems found at startup, shown once notifications exist.</summary>
    internal bool SettingsWereCorrupt { get; private set; }
    internal HotkeySetting? FailedHotkey { get; private set; }
    internal bool PrintScreenTakenBySnipping { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new Mutex(initiallyOwned: true, @"Local\sScreentify.SingleInstance", out var createdNew);
        if (!createdNew)
        {
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        _store = new SettingsStore(AppPaths.SettingsFile);
        var loaded = _store.Load();
        Settings = loaded.Settings;
        SettingsWereCorrupt = loaded.WasCorrupt;
        if (SettingsWereCorrupt) Debug.WriteLine(Strings.SettingsCorrupt);

        _overlay = new OverlayController(() => Settings, SaveSettings);
        _overlay.Prewarm();
        _export = new ExportActions(_overlay, () => Settings, SaveSettings, _toasts);

        _hotkeys = new HotkeyService();
        _hotkeys.Pressed += OnHotkey;
        ApplyHotkeys();

        _tray = new TrayIcon(string.Format(Strings.TrayTooltip, HotkeyRules.Display(Settings.MainHotkey)));
        _tray.CaptureRequested += StartCapture;
        _tray.ExitRequested += Shutdown;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _hotkeys?.Dispose();
        _singleInstance?.ReleaseMutex();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    internal void SaveSettings(AppSettings settings)
    {
        Settings = settings;
        _store.Save(settings);
    }

    private void ApplyHotkeys()
    {
        FailedHotkey = null;
        if (!_hotkeys!.Register(HotkeyId.Main, Settings.MainHotkey)) FailedHotkey = Settings.MainHotkey;
        if (!_hotkeys.Register(HotkeyId.SaveFullscreen, Settings.SaveFullscreenHotkey)) FailedHotkey ??= Settings.SaveFullscreenHotkey;

        var main = Settings.MainHotkey;
        var usesBarePrintScreen = main.Enabled && main.Modifiers == HotkeyModifiers.None && main.VirtualKey == HotkeyRules.VkSnapshot;
        PrintScreenTakenBySnipping = usesBarePrintScreen && SnippingToolConflict.IsPrintScreenTaken(
            Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Keyboard", "PrintScreenKeyForSnippingEnabled", null));
    }

    private void OnHotkey(HotkeyId id)
    {
        if (id == HotkeyId.Main) StartCapture();
        else if (id == HotkeyId.SaveFullscreen) _export?.SaveFullscreenInstant();
    }

    private void StartCapture() => _overlay?.Open();
}
