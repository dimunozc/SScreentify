using System.Windows.Interop;
using System.Windows.Threading;
using SScreentify.App.Native;
using SScreentify.Core.Hotkeys;

namespace SScreentify.App.Services;

public enum HotkeyId { Main = 1, SaveFullscreen = 2 }

/// <summary>
/// Global hotkeys. Impr Pant combinations are read through a low-level keyboard hook that keeps the key from Windows,
/// even when Windows 11 reserves it for the Snipping Tool; the rest use RegisterHotKey on a hidden message-only window.
/// <see cref="Pressed"/> is raised on the thread that created the service.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private static readonly IntPtr HwndMessage = new(-3);
    private readonly HwndSource _source;
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
    private readonly HashSet<HotkeyId> _registered = new();
    private readonly HashSet<HotkeyId> _intercepted = new();
    private readonly KeyInterceptor _interceptor = new();
    private KeyboardHook? _hook;

    public HotkeyService()
    {
        _source = new HwndSource(new HwndSourceParameters("sScreentify.Hotkeys") { ParentWindow = HwndMessage, Width = 0, Height = 0 });
        _source.AddHook(WndProc);
    }

    public event Action<HotkeyId>? Pressed;

    /// <summary>Replaces any hotkey with this id. A disabled setting only unregisters and reports success.</summary>
    public bool Register(HotkeyId id, HotkeySetting s)
    {
        Unregister(id);
        if (!s.Enabled)
            return true;

        if (KeyInterceptor.Intercepts(s))
        {
            // Installed on first use and kept until Dispose; with no bindings it passes every key straight through.
            _hook ??= KeyboardHook.TryStart(_interceptor, fired => _dispatcher.BeginInvoke(() => Pressed?.Invoke((HotkeyId)fired)));
            if (_hook is null)
                return false;
            _interceptor.Bind((int)id, s);
            _intercepted.Add(id);
            return true;
        }

        var modifiers = (uint)s.Modifiers | User32.MOD_NOREPEAT;
        if (!User32.RegisterHotKey(_source.Handle, (int)id, modifiers, (uint)s.VirtualKey))
            return false;
        _registered.Add(id);
        return true;
    }

    public void Unregister(HotkeyId id)
    {
        if (_intercepted.Remove(id))
            _interceptor.Unbind((int)id);
        if (_registered.Remove(id))
            User32.UnregisterHotKey(_source.Handle, (int)id);
    }

    public void Dispose()
    {
        foreach (var id in _registered.Concat(_intercepted).ToArray())
            Unregister(id);
        _hook?.Dispose();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == User32.WM_HOTKEY && Enum.IsDefined(typeof(HotkeyId), wParam.ToInt32()))
        {
            handled = true;
            Pressed?.Invoke((HotkeyId)wParam.ToInt32());
        }
        return IntPtr.Zero;
    }
}
