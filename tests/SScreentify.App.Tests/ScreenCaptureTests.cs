using SScreentify.App.Capture;
using SScreentify.App.Imaging;
using SScreentify.Core.Geometry;
using SScreentify.Core.Imaging;

namespace SScreentify.App.Tests;

public class ScreenCaptureTests
{
    [Fact]
    public void Capture_MatchesVirtualScreenSize()
    {
        var monitors = Monitors.Enumerate();
        Assert.NotEmpty(monitors);
        int left = monitors.Min(m => m.Bounds.X), top = monitors.Min(m => m.Bounds.Y);
        int right = monitors.Max(m => m.Bounds.Right), bottom = monitors.Max(m => m.Bounds.Bottom);

        var capture = ScreenCapture.CaptureVirtualScreen(includeCursor: false);

        Assert.Equal(new PixelRect(left, top, right - left, bottom - top), capture.VirtualBounds);
        Assert.Equal(capture.VirtualBounds.Width, capture.Pixels.Width);
        Assert.Equal(capture.VirtualBounds.Height, capture.Pixels.Height);
        Assert.Equal(capture.Pixels.Width * capture.Pixels.Height * 4, capture.Pixels.Bgra.Length);
        Assert.Single(capture.Monitors, m => m.IsPrimary);
    }

    [Fact]
    public void Capture_AlphaIsOpaque()
    {
        var capture = ScreenCapture.CaptureVirtualScreen(includeCursor: true);
        var random = new Random(1);
        for (var i = 0; i < 1000; i++)
        {
            var p = capture.Pixels.GetPixel(random.Next(capture.Pixels.Width), random.Next(capture.Pixels.Height));
            Assert.Equal(255, p.A);
        }
    }

    [Fact]
    public void PixelBufferWpf_RoundTrip()
    {
        var bytes = Enumerable.Range(0, 3 * 2 * 4).Select(i => (byte)(i * 10)).ToArray();
        for (var i = 3; i < bytes.Length; i += 4) bytes[i] = 255;
        var buffer = new PixelBuffer(3, 2, bytes);

        StaRunner.Run(() =>
        {
            var source = buffer.ToBitmapSource();
            Assert.Equal(96, source.DpiX);
            Assert.True(source.IsFrozen);
            Assert.Equal(bytes, source.ToPixelBuffer().Bgra);
        });
    }
}
