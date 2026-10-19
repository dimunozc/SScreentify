using System.Windows;
using System.Windows.Threading;
using SScreentify.App.Diagnostics;
using SScreentify.App.Export;
using SScreentify.App.Overlay;
using SScreentify.App.Services;
using SScreentify.App.Ui;
using SScreentify.Core.Hotkeys;
using SScreentify.Core.Settings;

namespace SScreentify.App;

public partial class App : Application
{
    private readonly ToastService _toasts = new();
    private Mutex? _singleInstance;
    private SettingsStore _store = null!;
    private TrayIcon? _tray;
    private HotkeyService? _hotkeys;
    private OverlayController? _overlay;
    private ExportActions? _export;
    private OptionsWindow? _options;
    private AboutWindow? _about;
    private HotkeySetting? _failedHotkey;

    internal AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;

        var exportIcon = Array.IndexOf(e.Args, "--export-icon");
        if (exportIcon >= 0 && exportIcon + 1 < e.Args.Length)
        {
            IconExport.Write(e.Args[exportIcon + 1]);
            Shutdown();
            return;
        }
        PerfTrace.Enabled = e.Args.Contains("--perf");
        // Measured (README): GPU rendering keeps ~70 MB of driver memory alive after the first capture while software
        // rendering stays under the 60 MB idle target with the same ~30 ms open time. --gpu-render opts back in.
        if (!e.Args.Contains("--gpu-render")) System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;

        // Kept lower-case on purpose: kernel object names are case-sensitive, so renaming it would let an older build run alongside.
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
        UiLanguage.Apply(Settings.Language);

        _overlay = new OverlayController(() => Settings, SaveSettings);
        _overlay.Prewarm();
        _overlay.Failed += ex => _toasts.Show(string.Format(Strings.ToastCaptureFailed, ex.Message), isError: true);
        _export = new ExportActions(_overlay, () => Settings, SaveSettings, _toasts);

        _hotkeys = new HotkeyService();
        _hotkeys.Pressed += OnHotkey;
        ApplyHotkeys();

        _tray = new TrayIcon(TrayTooltip());
        _tray.CaptureRequested += StartCapture;
        _tray.OptionsRequested += ShowOptions;
        _tray.AboutRequested += ShowAbout;
        _tray.ExitRequested += Shutdown;
        PerfTrace.Mark("tray-ready");
        if (PerfTrace.Enabled)
        {
            _overlay.SessionClosed += () => ScheduleMemorySample("idle-memory");
            ScheduleMemorySample("startup-memory");
        }

        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
        {
            if (loaded.WasCorrupt) _toasts.Show(Strings.SettingsCorrupt, isError: true);
            ShowHotkeyNotices();
        });
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
        _failedHotkey = null;
        if (!_hotkeys!.Register(HotkeyId.Main, Settings.MainHotkey)) _failedHotkey = Settings.MainHotkey;
        if (!_hotkeys.Register(HotkeyId.SaveFullscreen, Settings.SaveFullscreenHotkey)) _failedHotkey ??= Settings.SaveFullscreenHotkey;
    }

    private void ShowHotkeyNotices()
    {
        if (_failedHotkey is { } failed)
            _toasts.Show(string.Format(Strings.ToastHotkeyFailed, HotkeyRules.Display(failed, UiLanguage.Current)), onClick: ShowOptions, isError: true);
    }

    private void ShowOptions()
    {
        if (_options is not null)
        {
            _options.Activate();
            return;
        }

        // While recording shortcuts the global hotkeys must not fire.
        _hotkeys!.Unregister(HotkeyId.Main);
        _hotkeys.Unregister(HotkeyId.SaveFullscreen);
        _options = new OptionsWindow(Settings);
        try
        {
            if (_options.ShowDialog() == true && _options.Result is { } result)
            {
                SaveSettings(result);
                if (Environment.ProcessPath is { } exe) AutoStart.ForCurrentUser().Apply(result.StartWithWindows, exe);
                if (UiLanguage.Apply(result.Language))
                {
                    _tray?.RefreshTexts();
                    _overlay?.RefreshTexts();
                }
            }
        }
        finally
        {
            _options = null;
            ApplyHotkeys();
            _tray?.SetTooltip(TrayTooltip());
            ShowHotkeyNotices();
        }
    }

    private void ShowAbout()
    {
        if (_about is not null)
        {
            _about.Activate();
            return;
        }
        _about = new AboutWindow();
        _about.Closed += (_, _) => _about = null;
        _about.Show();
    }

    private string TrayTooltip() => string.Format(Strings.TrayTooltip, HotkeyRules.Display(Settings.MainHotkey, UiLanguage.Current));

    private void OnHotkey(HotkeyId id)
    {
        PerfTrace.Mark("hotkey", id.ToString());
        if (id == HotkeyId.Main) StartCapture();
        else if (id == HotkeyId.SaveFullscreen) _export?.SaveFullscreenInstant();
    }

    private void StartCapture() => _overlay?.Open();

    /// <summary>Last line of defence: a tray app must not vanish. Log, tell the user, keep running.</summary>
    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var log = Path.Combine(Path.GetDirectoryName(AppPaths.SettingsFile)!, "error.log");
            Directory.CreateDirectory(Path.GetDirectoryName(log)!);
            File.AppendAllText(log, $"{DateTime.Now:O} {e.Exception}{Environment.NewLine}");
            _toasts.Show(string.Format(Strings.ToastUnexpectedError, e.Exception.Message), isError: true);
        }
        catch (Exception)
        {
            // Reporting must never throw from the handler.
        }
        e.Handled = true;
    }

    private static void ScheduleMemorySample(string name)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        timer.Tick += (_, _) => { timer.Stop(); PerfTrace.Memory(name); };
        timer.Start();
    }
}
