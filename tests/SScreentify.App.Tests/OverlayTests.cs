using SScreentify.App.Overlay;
using SScreentify.Core.Geometry;

namespace SScreentify.App.Tests;

public class OverlayTests
{
    private static MonitorInfo Monitor(int x, int y, int w, int h, bool primary = false) =>
        new(new PixelRect(x, y, w, h), new PixelRect(x, y, w, h - 40), 1.0, primary);

    [Fact]
    public void CaptureGate_IgnoresReentry()
    {
        var gate = new CaptureGate();
        Assert.True(gate.TryEnter());
        Assert.False(gate.TryEnter());
        gate.Exit();
        Assert.True(gate.TryEnter());
    }

    [Fact]
    public void OverlayLayout_RebuildsFromMonitorList()
    {
        var single = new[] { Monitor(0, 0, 1920, 1080, primary: true) };
        var dual = new[] { Monitor(0, 0, 1920, 1080, primary: true), Monitor(-2560, -200, 2560, 1440) };

        Assert.Equal(new[] { new PixelRect(0, 0, 1920, 1080) }, OverlayLayout.WindowRects(single));
        Assert.Equal(new[] { new PixelRect(0, 0, 1920, 1080), new PixelRect(-2560, -200, 2560, 1440) }, OverlayLayout.WindowRects(dual));
    }

    [Fact]
    public void MonitorForSelection_UsesCenterThenLargestIntersection()
    {
        var left = Monitor(-1920, 0, 1920, 1080);
        var right = Monitor(0, 0, 1920, 1080, primary: true);
        var monitors = new[] { left, right };

        Assert.Same(left, OverlayLayout.MonitorForSelection(monitors, new PixelRect(-1000, 100, 1200, 200)));
        Assert.Same(right, OverlayLayout.MonitorForSelection(monitors, new PixelRect(-100, 100, 1200, 200)));

        // Center falls in a gap below the shorter monitor: pick the one with the larger overlap.
        var tall = Monitor(1920, 0, 1920, 2160);
        var withGap = new[] { right, tall };
        Assert.Same(tall, OverlayLayout.MonitorForSelection(withGap, new PixelRect(1800, 1000, 400, 400)));
    }

    [Fact]
    public void MonitorForSelection_FallsBackToPrimary()
    {
        var primary = Monitor(0, 0, 1920, 1080, primary: true);
        var other = Monitor(1920, 0, 1920, 1080);
        Assert.Same(primary, OverlayLayout.MonitorForSelection(new[] { other, primary }, new PixelRect(9000, 9000, 10, 10)));
    }
}
