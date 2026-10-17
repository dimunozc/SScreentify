using SScreentify.App.Native;
using SScreentify.Core.Geometry;

namespace SScreentify.App.Capture;

public static class Monitors
{
    private const int MDT_EFFECTIVE_DPI = 0;

    public static IReadOnlyList<MonitorInfo> Enumerate()
    {
        using var dpi = PhysicalDpiScope.Enter();
        var result = new List<MonitorInfo>();
        User32.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr handle, IntPtr _, ref User32.RECT _, IntPtr _) =>
        {
            var info = new User32.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<User32.MONITORINFO>() };
            if (!User32.GetMonitorInfo(handle, ref info))
                return true;
            var scale = User32.GetDpiForMonitor(handle, MDT_EFFECTIVE_DPI, out var dpiX, out _) == 0 ? dpiX / 96.0 : 1.0;
            result.Add(new MonitorInfo(ToRect(info.rcMonitor), ToRect(info.rcWork), scale,
                (info.dwFlags & User32.MONITORINFOF_PRIMARY) != 0));
            return true;
        }, IntPtr.Zero);
        return result;
    }

    private static PixelRect ToRect(User32.RECT r) => new(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
}
