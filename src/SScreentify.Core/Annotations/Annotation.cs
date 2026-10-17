using SScreentify.Core.Geometry;

namespace SScreentify.Core.Annotations;

public enum CensorStyle { Pixelate, Blur, Solid }

/// <summary>An editable drawing in physical capture coordinates (not selection coordinates).</summary>
public abstract record Annotation(Guid Id, Rgba Color, double Thickness)
{
    public abstract RectD Bounds { get; }
    public abstract Annotation Translate(double dx, double dy);
    public abstract Annotation ResizeTo(RectD bounds);
    public abstract bool HitTest(PointD p, double tolerance);

    /// <summary>Maps a point from the current bounds into <paramref name="target"/>.</summary>
    protected PointD MapInto(PointD p, RectD target)
    {
        var b = Bounds;
        var sx = b.Width == 0 ? 1 : target.Width / b.Width;
        var sy = b.Height == 0 ? 1 : target.Height / b.Height;
        return new PointD(target.X + (p.X - b.X) * sx, target.Y + (p.Y - b.Y) * sy);
    }

    internal static double DistanceToSegment(PointD p, PointD a, PointD b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        var lengthSquared = dx * dx + dy * dy;
        var t = lengthSquared == 0 ? 0 : Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lengthSquared, 0, 1);
        double cx = a.X + t * dx - p.X, cy = a.Y + t * dy - p.Y;
        return Math.Sqrt(cx * cx + cy * cy);
    }
}

public sealed record PenAnnotation(Guid Id, Rgba Color, double Thickness, IReadOnlyList<PointD> Points, bool IsMarker)
    : Annotation(Id, Color, Thickness)
{
    /// <summary>Markers are drawn four times wider than the base thickness.</summary>
    public double StrokeWidth => IsMarker ? Thickness * 4 : Thickness;

    public override RectD Bounds
    {
        get
        {
            if (Points.Count == 0) return default;
            double minX = Points.Min(p => p.X), minY = Points.Min(p => p.Y);
            return new RectD(minX, minY, Points.Max(p => p.X) - minX, Points.Max(p => p.Y) - minY);
        }
    }

    public override Annotation Translate(double dx, double dy) =>
        this with { Points = Points.Select(p => new PointD(p.X + dx, p.Y + dy)).ToArray() };

    public override Annotation ResizeTo(RectD bounds) =>
        this with { Points = Points.Select(p => MapInto(p, bounds)).ToArray() };

    public override bool HitTest(PointD p, double tolerance)
    {
        var limit = tolerance + StrokeWidth / 2;
        if (Points.Count == 1) return DistanceToSegment(p, Points[0], Points[0]) <= limit;
        for (var i = 1; i < Points.Count; i++)
            if (DistanceToSegment(p, Points[i - 1], Points[i]) <= limit)
                return true;
        return false;
    }
}

public sealed record SegmentAnnotation(Guid Id, Rgba Color, double Thickness, PointD Start, PointD End, bool HasArrowHead)
    : Annotation(Id, Color, Thickness)
{
    public override RectD Bounds => RectD.FromPoints(Start, End);

    public override Annotation Translate(double dx, double dy) =>
        this with { Start = new PointD(Start.X + dx, Start.Y + dy), End = new PointD(End.X + dx, End.Y + dy) };

    public override Annotation ResizeTo(RectD bounds) =>
        this with { Start = MapInto(Start, bounds), End = MapInto(End, bounds) };

    public override bool HitTest(PointD p, double tolerance) =>
        DistanceToSegment(p, Start, End) <= tolerance + Thickness / 2;
}

public sealed record ShapeAnnotation(Guid Id, Rgba Color, double Thickness, RectD Rect, bool IsEllipse)
    : Annotation(Id, Color, Thickness)
{
    public override RectD Bounds => Rect;

    public override Annotation Translate(double dx, double dy) => this with { Rect = Rect with { X = Rect.X + dx, Y = Rect.Y + dy } };

    public override Annotation ResizeTo(RectD bounds) => this with { Rect = bounds };

    public override bool HitTest(PointD p, double tolerance)
    {
        var limit = tolerance + Thickness / 2;
        if (!IsEllipse)
            return Rect.Inflate(limit).Contains(p) && !Rect.Inflate(-limit).Contains(p);

        double a = Rect.Width / 2, b = Rect.Height / 2;
        if (a <= 0 || b <= 0) return Rect.Inflate(limit).Contains(p);
        double nx = (p.X - (Rect.X + a)) / a, ny = (p.Y - (Rect.Y + b)) / b;
        var radial = Math.Sqrt(nx * nx + ny * ny);
        return Math.Abs(radial - 1) * Math.Min(a, b) <= limit;
    }
}

public sealed record TextAnnotation(Guid Id, Rgba Color, PointD Origin, string Text, double FontSize, RectD MeasuredBounds)
    : Annotation(Id, Color, 0)
{
    public override RectD Bounds => MeasuredBounds;

    public override Annotation Translate(double dx, double dy) => this with
    {
        Origin = new PointD(Origin.X + dx, Origin.Y + dy),
        MeasuredBounds = MeasuredBounds with { X = MeasuredBounds.X + dx, Y = MeasuredBounds.Y + dy },
    };

    /// <summary>Text keeps its aspect: font size scales with the new height.</summary>
    public override Annotation ResizeTo(RectD bounds)
    {
        var scale = MeasuredBounds.Height == 0 ? 1 : bounds.Height / MeasuredBounds.Height;
        return this with
        {
            Origin = MapInto(Origin, bounds),
            FontSize = FontSize * scale,
            MeasuredBounds = new RectD(bounds.X, bounds.Y, MeasuredBounds.Width * scale, bounds.Height),
        };
    }

    public override bool HitTest(PointD p, double tolerance) => MeasuredBounds.Inflate(tolerance).Contains(p);
}

public sealed record CensorAnnotation(Guid Id, Rgba Color, RectD Rect, CensorStyle Style)
    : Annotation(Id, Color, 0)
{
    public override RectD Bounds => Rect;

    public override Annotation Translate(double dx, double dy) => this with { Rect = Rect with { X = Rect.X + dx, Y = Rect.Y + dy } };

    public override Annotation ResizeTo(RectD bounds) => this with { Rect = bounds };

    public override bool HitTest(PointD p, double tolerance) => Rect.Inflate(tolerance).Contains(p);
}
