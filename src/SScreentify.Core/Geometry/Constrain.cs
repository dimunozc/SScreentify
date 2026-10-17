namespace SScreentify.Core.Geometry;

/// <summary>A sub-pixel point in physical capture coordinates.</summary>
public readonly record struct PointD(double X, double Y);

public readonly record struct RectD(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;

    public bool Contains(PointD p) => p.X >= X && p.X <= Right && p.Y >= Y && p.Y <= Bottom;

    public RectD Inflate(double amount) => new(X - amount, Y - amount, Width + 2 * amount, Height + 2 * amount);

    public static RectD FromPoints(PointD a, PointD b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
}

/// <summary>Shift-key constraints for drawing tools.</summary>
public static class Constrain
{
    public static PointD SnapAngle45(PointD origin, PointD p)
    {
        double dx = p.X - origin.X, dy = p.Y - origin.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        var step = Math.PI / 4;
        var angle = Math.Round(Math.Atan2(dy, dx) / step) * step;
        return new PointD(origin.X + length * Math.Cos(angle), origin.Y + length * Math.Sin(angle));
    }

    public static PointD SquareCorner(PointD origin, PointD p)
    {
        double dx = p.X - origin.X, dy = p.Y - origin.Y;
        var side = Math.Max(Math.Abs(dx), Math.Abs(dy));
        return new PointD(origin.X + side * Sign(dx), origin.Y + side * Sign(dy));
    }

    private static int Sign(double v) => v < 0 ? -1 : 1;
}
