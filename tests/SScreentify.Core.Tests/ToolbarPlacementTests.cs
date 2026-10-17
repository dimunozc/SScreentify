using SScreentify.Core.Geometry;

namespace SScreentify.Core.Tests;

public class ToolbarPlacementTests
{
    private static readonly PixelRect Monitor = new(0, 0, 1920, 1080);
    private static readonly PixelSize Tools = new(40, 380);
    private static readonly PixelSize Actions = new(200, 40);
    private const int Gap = 8;

    [Fact]
    public void PlaceVertical_RightWhenFits()
    {
        var r = ToolbarPlacement.PlaceVertical(new PixelRect(100, 100, 300, 200), Tools, Monitor, Gap);
        Assert.Equal(new PixelRect(408, 100, 40, 380), r);
    }

    [Fact]
    public void PlaceVertical_LeftWhenRightOverflows()
    {
        var r = ToolbarPlacement.PlaceVertical(new PixelRect(1800, 100, 100, 200), Tools, Monitor, Gap);
        Assert.Equal(1800 - 8 - 40, r.X);
    }

    [Fact]
    public void PlaceVertical_InsideWhenBothOverflow()
    {
        var r = ToolbarPlacement.PlaceVertical(Monitor, Tools, Monitor, Gap);
        Assert.Equal(Monitor.Right - Gap, r.Right);
        Assert.Equal(Gap, r.Y);
    }

    [Fact]
    public void PlaceVertical_ShiftsUpToFitMonitor()
    {
        var r = ToolbarPlacement.PlaceVertical(new PixelRect(100, 900, 300, 100), Tools, Monitor, Gap);
        Assert.True(r.Bottom <= 1080);
        Assert.Equal(1080 - 380, r.Y);
    }

    [Fact]
    public void PlaceVertical_RespectsMonitorOffset()
    {
        var second = new PixelRect(1920, -200, 1280, 1024);
        var r = ToolbarPlacement.PlaceVertical(new PixelRect(2000, -200, 200, 100), Tools, second, Gap);
        Assert.Equal(2208, r.X);
        Assert.Equal(-200, r.Y);
    }

    [Fact]
    public void PlaceHorizontal_BelowWhenFits()
    {
        var r = ToolbarPlacement.PlaceHorizontal(new PixelRect(100, 100, 300, 200), Actions, Monitor, Gap);
        Assert.Equal(new PixelRect(200, 308, 200, 40), r);
    }

    [Fact]
    public void PlaceHorizontal_AboveWhenBelowOverflows()
    {
        var r = ToolbarPlacement.PlaceHorizontal(new PixelRect(100, 1000, 300, 60), Actions, Monitor, Gap);
        Assert.Equal(1000 - 8 - 40, r.Y);
    }

    [Fact]
    public void PlaceHorizontal_InsideWhenBothOverflow()
    {
        var r = ToolbarPlacement.PlaceHorizontal(Monitor, Actions, Monitor, Gap);
        Assert.Equal(new PixelRect(1920 - 8 - 200, 1080 - 8 - 40, 200, 40), r);
    }

    [Fact]
    public void PlaceHorizontal_ShiftsRightToFitMonitor()
    {
        var r = ToolbarPlacement.PlaceHorizontal(new PixelRect(0, 100, 50, 50), Actions, Monitor, Gap);
        Assert.Equal(0, r.X);
    }
}
