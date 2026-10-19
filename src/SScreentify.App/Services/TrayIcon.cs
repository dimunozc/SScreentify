using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using SScreentify.App.Ui;
using Forms = System.Windows.Forms;

namespace SScreentify.App.Services;

/// <summary>Notification-area icon with the app menu.</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Icon _image;

    public TrayIcon(string tooltip)
    {
        _image = CreateIcon(Forms.SystemInformation.SmallIconSize.Width);
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(Strings.MenuCapture, null, (_, _) => CaptureRequested?.Invoke());
        menu.Items.Add(Strings.MenuOptions, null, (_, _) => OptionsRequested?.Invoke());
        menu.Items.Add(Strings.MenuAbout, null, (_, _) => AboutRequested?.Invoke());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(Strings.MenuExit, null, (_, _) => ExitRequested?.Invoke());

        _icon = new Forms.NotifyIcon { Icon = _image, ContextMenuStrip = menu, Visible = true };
        _icon.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) CaptureRequested?.Invoke(); };
        SetTooltip(tooltip);
    }

    public event Action? CaptureRequested, OptionsRequested, AboutRequested, ExitRequested;

    public void SetTooltip(string text) => _icon.Text = text.Length > 127 ? text[..127] : text;

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
        _image.Dispose();
    }

    /// <summary>Own mark: a selection frame with corner brackets in the accent blue.</summary>
    internal static Icon CreateIcon(int size)
    {
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var stroke = Math.Max(1.5f, size / 10f);
            var inset = stroke;
            var span = size - 2 * inset;
            var arm = span * 0.36f;
            using var pen = new Pen(Color.FromArgb(0x60, 0xA5, 0xFA), stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            float l = inset, t = inset, r = size - inset, b = size - inset;
            g.DrawLines(pen, new[] { new PointF(l, t + arm), new PointF(l, t), new PointF(l + arm, t) });
            g.DrawLines(pen, new[] { new PointF(r - arm, t), new PointF(r, t), new PointF(r, t + arm) });
            g.DrawLines(pen, new[] { new PointF(r, b - arm), new PointF(r, b), new PointF(r - arm, b) });
            g.DrawLines(pen, new[] { new PointF(l + arm, b), new PointF(l, b), new PointF(l, b - arm) });
            using var dot = new SolidBrush(Color.FromArgb(0xF4, 0x3F, 0x5E));
            var d = size * 0.28f;
            g.FillEllipse(dot, (size - d) / 2, (size - d) / 2, d, d);
        }

        var handle = bitmap.GetHicon();
        try { return (Icon)Icon.FromHandle(handle).Clone(); }
        finally { DestroyIcon(handle); }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
