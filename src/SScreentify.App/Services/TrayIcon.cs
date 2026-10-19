using System.Windows.Threading;
using Microsoft.Win32;
using SScreentify.App.Ui;
using Forms = System.Windows.Forms;

namespace SScreentify.App.Services;

/// <summary>Notification-area icon with the app menu. The mark follows the taskbar theme (white on dark, ink on light).</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    private readonly LocalizedTexts _texts = new();
    private System.Drawing.Icon _image;
    private bool _lightTaskbar, _disposed;

    public TrayIcon(string tooltip)
    {
        _lightTaskbar = IsTaskbarLight();
        _image = CreateImage();
        var menu = new Forms.ContextMenuStrip();
        foreach (var (text, click) in new (Func<string>, Action)[]
                 {
                     (() => Strings.MenuCapture, () => CaptureRequested?.Invoke()),
                     (() => Strings.MenuOptions, () => OptionsRequested?.Invoke()),
                     (() => Strings.MenuAbout, () => AboutRequested?.Invoke()),
                     (() => Strings.MenuExit, () => ExitRequested?.Invoke()),
                 })
        {
            if (menu.Items.Count == 3) menu.Items.Add(new Forms.ToolStripSeparator());
            var item = menu.Items.Add("", null, (_, _) => click());
            _texts.Add(() => item.Text = text());
        }

        _icon = new Forms.NotifyIcon { Icon = _image, ContextMenuStrip = menu, Visible = true };
        _icon.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) CaptureRequested?.Invoke(); };
        SetTooltip(tooltip);
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public event Action? CaptureRequested, OptionsRequested, AboutRequested, ExitRequested;

    public void SetTooltip(string text) => _icon.Text = text.Length > 127 ? text[..127] : text;

    /// <summary>Re-reads the menu texts after the interface language changed.</summary>
    public void RefreshTexts() => _texts.Refresh();

    public void Dispose()
    {
        _disposed = true;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
        _image.Dispose();
    }

    /// <summary>Windows reports the taskbar theme as Personalize\SystemUsesLightTheme = 1; anything else is the dark default.</summary>
    internal static bool IsTaskbarLight() =>
        Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "SystemUsesLightTheme", null) is int value
        && value == 1;

    private System.Drawing.Icon CreateImage() => BrandMark.TrayIcon(Forms.SystemInformation.SmallIconSize.Width, _lightTaskbar);

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        // Raised on a system thread; the icon belongs to the UI thread.
        if (e.Category == UserPreferenceCategory.General)
            _dispatcher.BeginInvoke(() =>
            {
                var light = IsTaskbarLight();
                if (_disposed || light == _lightTaskbar) return;
                _lightTaskbar = light;
                var old = _image;
                _image = CreateImage();
                _icon.Icon = _image;
                old.Dispose();
            });
    }
}
