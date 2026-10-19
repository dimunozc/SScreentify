using System.Runtime.InteropServices;
using SScreentify.App.Native;
using SScreentify.Core.Hotkeys;

namespace SScreentify.App.Services;

/// <summary>
/// A WH_KEYBOARD_LL hook on its own thread that feeds every key event to a <see cref="KeyInterceptor"/>.
/// Windows silently removes a low-level hook whose callback is slow, so the callback never waits on the UI thread:
/// it only asks the interceptor and hands matches to <c>fired</c>, which must return immediately.
/// </summary>
internal sealed class KeyboardHook : IDisposable
{
    private readonly KeyInterceptor _interceptor;
    private readonly Action<int> _fired;
    private readonly User32.LowLevelKeyboardProc _callback; // a field so the GC keeps it alive while installed
    private readonly Thread _thread;
    private IntPtr _hook;
    private uint _threadId;

    private KeyboardHook(KeyInterceptor interceptor, Action<int> fired)
    {
        _interceptor = interceptor;
        _fired = fired;
        _callback = Callback;
        _thread = new Thread(Run) { IsBackground = true, Name = "sScreentify.KeyboardHook", Priority = ThreadPriority.AboveNormal };
    }

    /// <summary>Installs the hook and returns null if Windows refuses it.</summary>
    public static KeyboardHook? TryStart(KeyInterceptor interceptor, Action<int> fired)
    {
        var hook = new KeyboardHook(interceptor, fired);
        using var ready = new ManualResetEventSlim();
        hook._thread.Start(ready);
        ready.Wait();
        if (hook._hook != IntPtr.Zero)
            return hook;
        hook._thread.Join();
        return null;
    }

    public void Dispose()
    {
        User32.PostThreadMessage(_threadId, User32.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _thread.Join();
    }

    private void Run(object? ready)
    {
        _threadId = Kernel32.GetCurrentThreadId();
        User32.PeekMessage(out _, IntPtr.Zero, 0, 0, User32.PM_NOREMOVE); // creates the queue that WM_QUIT is posted to
        _hook = User32.SetWindowsHookEx(User32.WH_KEYBOARD_LL, _callback, Kernel32.GetModuleHandle(null), 0);
        ((ManualResetEventSlim)ready!).Set();
        if (_hook == IntPtr.Zero)
            return;

        while (User32.GetMessage(out _, IntPtr.Zero, 0, 0) > 0) { }
        User32.UnhookWindowsHookEx(_hook);
    }

    private IntPtr Callback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            var vk = Marshal.ReadInt32(lParam); // KBDLLHOOKSTRUCT.vkCode
            var message = wParam.ToInt32();
            var decision = _interceptor.OnKey(vk, message is User32.WM_KEYDOWN or User32.WM_SYSKEYDOWN, HeldModifiers());
            if (decision.Fire is { } id)
                _fired(id);
            if (decision.Swallow)
                return 1;
        }
        return User32.CallNextHookEx(_hook, code, wParam, lParam);
    }

    private static HotkeyModifiers HeldModifiers()
    {
        static bool Down(int vk) => (User32.GetAsyncKeyState(vk) & 0x8000) != 0;
        var m = HotkeyModifiers.None;
        if (Down(User32.VK_CONTROL)) m |= HotkeyModifiers.Control;
        if (Down(User32.VK_MENU)) m |= HotkeyModifiers.Alt;
        if (Down(User32.VK_SHIFT)) m |= HotkeyModifiers.Shift;
        if (Down(User32.VK_LWIN) || Down(User32.VK_RWIN)) m |= HotkeyModifiers.Win;
        return m;
    }
}
