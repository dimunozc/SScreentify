using System.ComponentModel;
using System.Runtime.InteropServices;
using SScreentify.App.Native;
using SScreentify.Core.Geometry;
using SScreentify.Core.Imaging;

namespace SScreentify.App.Capture;

public sealed record CaptureResult(PixelBuffer Pixels, PixelRect VirtualBounds, IReadOnlyList<MonitorInfo> Monitors);

/// <summary>Freezes the whole virtual desktop in physical pixels with GDI.</summary>
public static class ScreenCapture
{
    public static CaptureResult CaptureVirtualScreen(bool includeCursor)
    {
        using var dpi = PhysicalDpiScope.Enter();
        var bounds = new PixelRect(
            User32.GetSystemMetrics(User32.SM_XVIRTUALSCREEN), User32.GetSystemMetrics(User32.SM_YVIRTUALSCREEN),
            User32.GetSystemMetrics(User32.SM_CXVIRTUALSCREEN), User32.GetSystemMetrics(User32.SM_CYVIRTUALSCREEN));

        var screenDc = User32.GetDC(IntPtr.Zero);
        var memoryDc = Gdi32.CreateCompatibleDC(screenDc);
        var header = new Gdi32.BITMAPINFOHEADER
        {
            biSize = Marshal.SizeOf<Gdi32.BITMAPINFOHEADER>(),
            biWidth = bounds.Width,
            biHeight = -bounds.Height, // top-down
            biPlanes = 1,
            biBitCount = 32,
        };
        var bitmap = Gdi32.CreateDIBSection(screenDc, ref header, Gdi32.DIB_RGB_COLORS, out var bits, IntPtr.Zero, 0);
        try
        {
            // These messages reach the user through ToastCaptureFailed, so they are localized.
            if (bitmap == IntPtr.Zero) throw new Win32Exception(Ui.Strings.ErrorCaptureBitmap);
            var previous = Gdi32.SelectObject(memoryDc, bitmap);
            if (!Gdi32.BitBlt(memoryDc, 0, 0, bounds.Width, bounds.Height, screenDc, bounds.X, bounds.Y, Gdi32.SRCCOPY | Gdi32.CAPTUREBLT))
                throw new Win32Exception(Marshal.GetLastWin32Error(), Ui.Strings.ErrorCaptureCopy);
            if (includeCursor) DrawCursor(memoryDc, bounds);
            Gdi32.SelectObject(memoryDc, previous);

            var pixels = new byte[bounds.Width * bounds.Height * 4];
            Marshal.Copy(bits, pixels, 0, pixels.Length);
            for (var i = 3; i < pixels.Length; i += 4) pixels[i] = 255;

            return new CaptureResult(new PixelBuffer(bounds.Width, bounds.Height, pixels), bounds, Monitors.Enumerate());
        }
        finally
        {
            if (bitmap != IntPtr.Zero) Gdi32.DeleteObject(bitmap);
            Gdi32.DeleteDC(memoryDc);
            User32.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private static void DrawCursor(IntPtr dc, PixelRect bounds)
    {
        var info = new User32.CURSORINFO { cbSize = Marshal.SizeOf<User32.CURSORINFO>() };
        if (!User32.GetCursorInfo(ref info) || (info.flags & User32.CURSOR_SHOWING) == 0 || info.hCursor == IntPtr.Zero)
            return;

        int hotX = 0, hotY = 0;
        if (User32.GetIconInfo(info.hCursor, out var icon))
        {
            (hotX, hotY) = (icon.xHotspot, icon.yHotspot);
            if (icon.hbmMask != IntPtr.Zero) Gdi32.DeleteObject(icon.hbmMask);
            if (icon.hbmColor != IntPtr.Zero) Gdi32.DeleteObject(icon.hbmColor);
        }

        User32.DrawIconEx(dc, info.ptScreenPos.X - bounds.X - hotX, info.ptScreenPos.Y - bounds.Y - hotY,
            info.hCursor, 0, 0, 0, IntPtr.Zero, User32.DI_NORMAL);
    }
}
