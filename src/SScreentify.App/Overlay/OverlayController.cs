using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using SScreentify.App.Capture;
using SScreentify.App.Diagnostics;
using SScreentify.App.Native;
using SScreentify.App.Rendering;
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
    private readonly ChromeController _chrome = new();
    private readonly TextEditorHost _editor = new();
    private ToolController? _tools;
    private bool _creating;
    private bool _dragging;
    private bool _drawing;

    public OverlayController(Func<AppSettings> settings, Action<AppSettings> saveSettings)
    {
        _settings = settings;
        _saveSettings = saveSettings;
        Dispatcher = Dispatcher.CurrentDispatcher;
        // Raised on a system thread: marshal back to the UI thread.
        SystemEvents.DisplaySettingsChanged += (_, _) => Dispatcher.BeginInvoke(Rebuild);
        _chrome.Actions.CopyRequested += () => { if (Session is { } s) CopyRequested?.Invoke(s); };
        _chrome.Actions.SaveRequested += () => { if (Session is { } s) SaveRequested?.Invoke(s); };
        _chrome.Actions.PrintRequested += () => { if (Session is { } s) PrintRequested?.Invoke(s); };
        _chrome.Actions.CloseRequested += Close;
    }

    public Dispatcher Dispatcher { get; }
    public CaptureSession? Session { get; private set; }
    public IReadOnlyList<OverlayWindow> Windows => _windows;
    public ChromeController Chrome => _chrome;

    /// <summary>The window of the selection's monitor (owner for dialogs).</summary>
    public OverlayWindow? HostWindow =>
        Session?.Selection.Rect is { } r ? WindowAt(new PointD(r.Center.X, r.Center.Y)) : _windows.FirstOrDefault();

    public event Action<CaptureSession>? SessionOpened;
    public event Action? SessionClosed;
    public event Action<CaptureSession>? CopyRequested, SaveRequested, PrintRequested;

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
            PerfTrace.Mark("captured", $"{capture.VirtualBounds.Width}x{capture.VirtualBounds.Height}");
            if (!OverlayLayout.WindowRects(capture.Monitors).SequenceEqual(_windows.Select(w => w.Monitor.Bounds)))
                RebuildWindows();

            var session = new CaptureSession(capture, settings);
            Session = session;
            _tools = new ToolController(session);
            _tools.Changed += RedrawAnnotations;
            _tools.TextEditRequested += BeginTextEdit;
            _tools.IndicatorRequested += ShowIndicator;
            session.Selection.Changed += OnSelectionChanged;
            session.ToolChanged += OnToolChanged;
            session.Document.Changed += RedrawAnnotations;
            _chrome.Attach(session);
            if (settings.KeepSelection && settings.LastSelection is { } last
                && capture.VirtualBounds.Intersect(last) == last)
                session.Selection.Set(last);

            var patches = new CensorPatchCache(capture.Pixels, capture.VirtualBounds);
            foreach (var window in _windows)
            {
                window.Attach(session, _tools, patches);
                window.ShowOverlay();
            }
            UpdateChrome();
            ActivateWindowUnderCursor();
            if (PerfTrace.Enabled) MarkFirstFrame();
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
        _creating = _dragging = _drawing = false;
        _editor.Cancel();
        _chrome.Detach();
        foreach (var window in _windows)
        {
            window.Hide();
            window.Detach();
        }
        if (session is not null)
        {
            session.Selection.Changed -= OnSelectionChanged;
            session.ToolChanged -= OnToolChanged;
            session.Document.Changed -= RedrawAnnotations;
            _tools = null;
            _saveSettings(session.ApplyTo(_settings()));
            SessionClosed?.Invoke();
            // The capture lives mostly in native WIC/WPF memory that the GC does not feel; reclaim it once idle.
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, ReleaseCaptureMemory);
        }
        _gate.Exit();
    }

    private void ReleaseCaptureMemory()
    {
        if (Session is not null) return;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static void MarkFirstFrame()
    {
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            System.Windows.Media.CompositionTarget.Rendering -= handler;
            PerfTrace.Mark("rendered");
        };
        System.Windows.Media.CompositionTarget.Rendering += handler;
    }

    /// <summary>Commits any text being typed so exports include it.</summary>
    public void FinishEditing() => _editor.Commit();

    internal void OnMouseDown(OverlayWindow window, PointD p, MouseButton button, ModifierKeys modifiers)
    {
        if (Session is not { } s) return;
        var point = ToPixel(p);

        if (_editor.IsEditing)
        {
            _editor.Commit();
            return;
        }

        if (button == MouseButton.Right)
        {
            if (s.Selection.Rect is not { } r || !r.Contains(point)) Close();
            return;
        }
        if (button != MouseButton.Left) return;

        if (s.Tool != ToolKind.None)
        {
            _drawing = true;
            _tools?.OnMouseDown(p, modifiers);
            return;
        }

        var handle = s.Selection.HitTest(point, HandleRadius(window));
        if (handle == Handle.None)
        {
            _creating = true;
            _chrome.Hide();
            s.Selection.BeginCreate(point);
        }
        else
        {
            _dragging = true;
            _chrome.Hide();
            s.Selection.BeginDrag(handle, point);
        }
    }

    internal void OnMouseMove(OverlayWindow window, PointD p, ModifierKeys modifiers)
    {
        if (Session is not { } s) return;
        var point = ToPixel(p);
        if (_drawing) _tools?.OnMouseMove(p, modifiers);
        else if (_creating) s.Selection.UpdateCreate(point);
        else if (_dragging) s.Selection.UpdateDrag(point);
        else window.Cursor = s.Tool switch
        {
            ToolKind.None => CursorFor(s.Selection.HitTest(point, HandleRadius(window))),
            ToolKind.Text => Cursors.IBeam,
            ToolKind.Select => s.Document.HitTest(p, Math.Max(4, s.Thickness)) is null ? Cursors.Arrow : Cursors.SizeAll,
            _ => Cursors.Cross,
        };
    }

    internal void OnMouseUp(OverlayWindow window, PointD p, MouseButton button, ModifierKeys modifiers)
    {
        if (Session is not { } s || button != MouseButton.Left) return;
        if (_drawing)
        {
            _drawing = false;
            _tools?.OnMouseUp(p, modifiers);
            return;
        }
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
        UpdateChrome();
    }

    internal void OnMouseWheel(OverlayWindow window, int delta)
    {
        if (Session is { Tool: not ToolKind.None }) _tools?.OnWheel(delta);
    }

    internal bool OnKeyDown(OverlayWindow window, Key key, ModifierKeys modifiers)
    {
        if (Session is not { } s) return false;

        if (_editor.IsEditing)
        {
            // The text box owns typing; Esc confirms the text instead of closing the selector.
            if (key != Key.Escape) return false;
            _editor.Commit();
            return true;
        }

        if (_tools?.OnKey(key, modifiers) == true) return true;

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
            case Key.C when ctrl:
                CopyRequested?.Invoke(s);
                return true;
            case Key.S when ctrl:
                SaveRequested?.Invoke(s);
                return true;
            case Key.P when ctrl:
                PrintRequested?.Invoke(s);
                return true;
            case Key.X when ctrl:
                Close();
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

    private void BeginTextEdit(PointD p)
    {
        if (Session is not { } s || _tools is not { } tools) return;
        _drawing = false;
        _editor.Begin(WindowAt(p), p, s, tools.CommitText);
    }

    private void ShowIndicator(string text)
    {
        User32.GetCursorPos(out var cursor);
        var p = new PointD(cursor.X, cursor.Y);
        WindowAt(p).ShowIndicator(text, p);
    }

    private OverlayWindow WindowAt(PointD p) =>
        _windows.FirstOrDefault(w => w.Monitor.Bounds.Contains(ToPixel(p))) ?? _windows[0];

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

    private void OnSelectionChanged()
    {
        RedrawAll();
        if (!_creating && !_dragging) UpdateChrome();
    }

    private void OnToolChanged()
    {
        _editor.Commit();
        RedrawAll();
        UpdateChrome();
    }

    private void UpdateChrome()
    {
        if (Session is { } s) _chrome.Update(s, _windows);
    }

    private void RedrawAll()
    {
        foreach (var window in _windows) window.Redraw();
    }

    private void RedrawAnnotations()
    {
        foreach (var window in _windows) window.RedrawAnnotations();
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
