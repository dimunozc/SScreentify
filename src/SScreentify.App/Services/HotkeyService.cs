using System.Windows.Interop;
using SScreentify.App.Native;
using SScreentify.Core.Hotkeys;

namespace SScreentify.App.Services;

public enum HotkeyId { Main = 1, SaveFullscreen = 2 }

/// <summary>Global hotkeys delivered through a hidden message-only window.</summary>
public sealed class HotkeyService : IDisposable
{
    private static readonly IntPtr HwndMessage = new(-3);
    private readonly HwndSource _source;
    private readonly HashSet<HotkeyId> _registered = new();

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

        var modifiers = (uint)s.Modifiers | User32.MOD_NOREPEAT;
        if (!User32.RegisterHotKey(_source.Handle, (int)id, modifiers, (uint)s.VirtualKey))
            return false;
        _registered.Add(id);
        return true;
    }

    public void Unregister(HotkeyId id)
    {
        if (_registered.Remove(id))
            User32.UnregisterHotKey(_source.Handle, (int)id);
    }

    public void Dispose()
    {
        foreach (var id in _registered.ToArray())
            Unregister(id);
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
