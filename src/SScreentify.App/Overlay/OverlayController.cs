using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using SScreentify.App.Capture;
using SScreentify.App.Native;
using SScreentify.Core.Geometry;
using SScreentify.Core.Settings;

namespace SScreentify.App.Overlay;

/// <summary>Owns the pre-created overlay windows and the lifetime of each capture session.</summary>
public sealed class OverlayController
{
    private const double HandleHitDip = 6;

    private readonly Func<AppSettings> _settings;
    private readonly Action<AppSettings> _saveSettings;
    private readonly CaptureGate _gate = new();
    private readonly List<OverlayWindow> _windows = new();
    private bool _creating;
    private bool _dragging;

    public OverlayController(Func<AppSettings> settings, Action<AppSettings> saveSettings)
    {
        _settings = settings;
        _saveSettings = saveSettings;
        Dispatcher = Dispatcher.CurrentDispatcher;
        // Raised on a system thread: marshal back to the UI thread.
        SystemEvents.DisplaySettingsChanged += (_, _) => Dispatcher.BeginInvoke(Rebuild);
    }

    public Dispatcher Dispatcher { get; }
    public CaptureSession? Session { get; private set; }
    public IReadOnlyList<OverlayWindow> Windows => _windows;

    public event Action<CaptureSession>? SessionOpened;
    public event Action? SessionClosed;

    public void Prewarm()
    {
        foreach (var monitor in Monitors.Enumerate())
        {
            var window = new OverlayWindow(this, monitor);
            window.Prepare();
            _windows.Add(window);
        }
    }

    public void Open()
    {
        if (!_gate.TryEnter()) return;
        try
        {
            var settings = _settings();
            var capture = ScreenCapture.CaptureVirtualScreen(settings.CaptureCursor);
            if (!OverlayLayout.WindowRects(capture.Monitors).SequenceEqual(_windows.Select(w => w.Monitor.Bounds)))
                RebuildWindows();

            var session = new CaptureSession(capture, settings);
            Session = session;
            session.Selection.Changed += RedrawAll;
            session.ToolChanged += RedrawAll;
            if (settings.KeepSelection && settings.LastSelection is { } last
                && capture.VirtualBounds.Intersect(last) == last)
                session.Selection.Set(last);

            foreach (var window in _windows)
            {
                window.Attach(session);
                window.ShowOverlay();
            }
            ActivateWindowUnderCursor();
            SessionOpened?.Invoke(session);
        }
        catch
        {
            Close();
            throw;
        }
    }

    public void Close()
    {
        var session = Session;
        Session = null;
        _creating = _dragging = false;
        foreach (var window in _windows)
        {
            window.Hide();
            window.Detach();
        }
        if (session is not null)
        {
            session.Selection.Changed -= RedrawAll;
            session.ToolChanged -= RedrawAll;
            _saveSettings(session.ApplyTo(_settings()));
            SessionClosed?.Invoke();
        }
        _gate.Exit();
    }

    internal void OnMouseDown(OverlayWindow window, PointD p, MouseButton button, ModifierKeys modifiers)
    {
        if (Session is not { } s) return;
        var point = ToPixel(p);

        if (button == MouseButton.Right)
        {
            if (s.Selection.Rect is not { } r || !r.Contains(point)) Close();
            return;
        }
        if (button != MouseButton.Left || s.Tool != ToolKind.None) return;

        var handle = s.Selection.HitTest(point, HandleRadius(window));
        if (handle == Handle.None)
        {
            _creating = true;
            s.Selection.BeginCreate(point);
        }
        else
        {
            _dragging = true;
            s.Selection.BeginDrag(handle, point);
        }
    }

    internal void OnMouseMove(OverlayWindow window, PointD p, ModifierKeys modifiers)
    {
        if (Session is not { } s) return;
        var point = ToPixel(p);
        if (_creating) s.Selection.UpdateCreate(point);
        else if (_dragging) s.Selection.UpdateDrag(point);
        else if (s.Tool == ToolKind.None) window.Cursor = CursorFor(s.Selection.HitTest(point, HandleRadius(window)));
    }

    internal void OnMouseUp(OverlayWindow window, PointD p, MouseButton button, ModifierKeys modifiers)
    {
        if (Session is not { } s || button != MouseButton.Left) return;
        if (_creating)
        {
            _creating = false;
            if (!s.Selection.IsUsable) s.Selection.Clear();
        }
        if (_dragging)
        {
            _dragging = false;
            s.Selection.EndDrag();
        }
    }

    internal void OnMouseWheel(OverlayWindow window, int delta)
    {
    }

    internal bool OnKeyDown(OverlayWindow window, Key key, ModifierKeys modifiers)
    {
        if (Session is not { } s) return false;
        var ctrl = modifiers.HasFlag(ModifierKeys.Control);
        var shift = modifiers.HasFlag(ModifierKeys.Shift);

        switch (key)
        {
            case Key.Escape:
                Close();
                return true;
            case Key.A when ctrl:
                SelectFullScreen(s);
                return true;
            case Key.Left: s.Selection.Nudge(-1, 0, shift); return true;
            case Key.Right: s.Selection.Nudge(1, 0, shift); return true;
            case Key.Up: s.Selection.Nudge(0, -1, shift); return true;
            case Key.Down: s.Selection.Nudge(0, 1, shift); return true;
            default:
                return false;
        }
    }

    /// <summary>First press: the monitor under the cursor. Pressed again on that monitor: the whole virtual desktop.</summary>
    private static void SelectFullScreen(CaptureSession s)
    {
        User32.GetCursorPos(out var cursor);
        var under = s.Capture.Monitors.FirstOrDefault(m => m.Bounds.Contains(new PixelPoint(cursor.X, cursor.Y)))
                    ?? s.Capture.Monitors[0];
        s.Selection.Set(s.Selection.Rect == under.Bounds ? s.Capture.VirtualBounds : under.Bounds);
    }

    private void Rebuild()
    {
        Close();
        RebuildWindows();
    }

    private void RebuildWindows()
    {
        foreach (var window in _windows) window.Close();
        _windows.Clear();
        Prewarm();
    }

    private void RedrawAll()
    {
        foreach (var window in _windows) window.Redraw();
    }

    private void ActivateWindowUnderCursor()
    {
        User32.GetCursorPos(out var cursor);
        var target = _windows.FirstOrDefault(w => w.Monitor.Bounds.Contains(new PixelPoint(cursor.X, cursor.Y))) ?? _windows.FirstOrDefault();
        target?.Activate();
        target?.Focus();
    }

    private static int HandleRadius(OverlayWindow window) => (int)Math.Ceiling(HandleHitDip * window.Scale);

    private static PixelPoint ToPixel(PointD p) => new((int)Math.Round(p.X), (int)Math.Round(p.Y));

    private static Cursor CursorFor(Handle h) => h switch
    {
        Handle.TopLeft or Handle.BottomRight => Cursors.SizeNWSE,
        Handle.TopRight or Handle.BottomLeft => Cursors.SizeNESW,
        Handle.Top or Handle.Bottom => Cursors.SizeNS,
        Handle.Left or Handle.Right => Cursors.SizeWE,
        Handle.Move => Cursors.SizeAll,
        _ => Cursors.Cross,
    };
}
