using SScreentify.Core.Geometry;

namespace SScreentify.Core.Tests;

public class ConstrainTests
{
    [Fact]
    public void SnapAngle45_RoundsToNearest()
    {
        var flat = Constrain.SnapAngle45(new PointD(0, 0), new PointD(100, 10));
        Assert.Equal(Math.Sqrt(10100), flat.X, 6);
        Assert.Equal(0, flat.Y, 6);

        var diagonal = Constrain.SnapAngle45(new PointD(0, 0), new PointD(100, 90));
        var length = Math.Sqrt(100 * 100 + 90 * 90);
        Assert.Equal(length / Math.Sqrt(2), diagonal.X, 6);
        Assert.Equal(length / Math.Sqrt(2), diagonal.Y, 6);
    }

    [Fact]
    public void SnapAngle45_IsRelativeToOrigin()
    {
        var p = Constrain.SnapAngle45(new PointD(50, 50), new PointD(50, -10));
        Assert.Equal(50, p.X, 6);
        Assert.Equal(-10, p.Y, 6);
    }

    [Fact]
    public void SquareCorner_KeepsSigns()
    {
        Assert.Equal(new PointD(-30, 30), Constrain.SquareCorner(new PointD(0, 0), new PointD(-30, 10)));
        Assert.Equal(new PointD(15, -5), Constrain.SquareCorner(new PointD(10, 0), new PointD(12, -5)));
    }
}
