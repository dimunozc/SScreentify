using System.Globalization;
using System.Windows;
using System.Windows.Media;
using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;

namespace SScreentify.App.Rendering;

/// <summary>Draws annotations in capture coordinates (1 unit = 1 physical pixel). Shared by the overlay and export.</summary>
public static class AnnotationRenderer
{
    private const double ArrowHeadAngle = 28 * Math.PI / 180;
    private const double MarkerOpacity = 0.4;
    private static readonly Typeface TextFace = new(new FontFamily("Segoe UI Variable Text, Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

    public static void Draw(DrawingContext dc, Annotation a, CensorPatchCache? patches)
    {
        switch (a)
        {
            case PenAnnotation pen: DrawPen(dc, pen); break;
            case SegmentAnnotation segment: DrawSegment(dc, segment); break;
            case ShapeAnnotation shape: DrawShape(dc, shape); break;
            case TextAnnotation text: dc.DrawText(Format(text.Text, text.FontSize, text.Color), new Point(text.Origin.X, text.Origin.Y)); break;
            case CensorAnnotation censor: DrawCensor(dc, censor, patches); break;
        }
    }

    /// <summary>Size of the text block when drawn at the origin.</summary>
    public static RectD MeasureText(string text, double fontSize)
    {
        var formatted = Format(text, fontSize, new Rgba(0, 0, 0));
        return new RectD(0, 0, formatted.WidthIncludingTrailingWhitespace, formatted.Height);
    }

    public static Brush BrushOf(Rgba c)
    {
        var brush = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
        brush.Freeze();
        return brush;
    }

    private static FormattedText Format(string text, double fontSize, Rgba color) =>
        new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, TextFace, fontSize, BrushOf(color), pixelsPerDip: 1);

    private static void DrawPen(DrawingContext dc, PenAnnotation pen)
    {
        if (pen.Points.Count == 0) return;
        var stroke = new Pen(BrushOf(pen.Color), pen.StrokeWidth)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = pen.IsMarker ? PenLineCap.Flat : PenLineCap.Round,
            EndLineCap = pen.IsMarker ? PenLineCap.Flat : PenLineCap.Round,
        };

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var points = pen.Points;
            ctx.BeginFigure(ToPoint(points[0]), isFilled: false, isClosed: false);
            if (points.Count == 1)
                ctx.LineTo(ToPoint(points[0]), isStroked: true, isSmoothJoin: true);
            // Quadratic smoothing through segment midpoints.
            for (var i = 1; i < points.Count - 1; i++)
            {
                var mid = new Point((points[i].X + points[i + 1].X) / 2, (points[i].Y + points[i + 1].Y) / 2);
                ctx.QuadraticBezierTo(ToPoint(points[i]), mid, isStroked: true, isSmoothJoin: true);
            }
            if (points.Count > 1)
                ctx.LineTo(ToPoint(points[^1]), isStroked: true, isSmoothJoin: true);
        }
        geometry.Freeze();

        if (pen.IsMarker) dc.PushOpacity(MarkerOpacity);
        dc.DrawGeometry(null, stroke, geometry);
        if (pen.IsMarker) dc.Pop();
    }

    private static void DrawSegment(DrawingContext dc, SegmentAnnotation s)
    {
        var brush = BrushOf(s.Color);
        var stroke = new Pen(brush, s.Thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (!s.HasArrowHead)
        {
            dc.DrawLine(stroke, ToPoint(s.Start), ToPoint(s.End));
            return;
        }

        double dx = s.End.X - s.Start.X, dy = s.End.Y - s.Start.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.5) return;
        var head = Math.Min(Math.Max(10, s.Thickness * 3.5), length);
        double ux = dx / length, uy = dy / length;
        var halfWidth = Math.Tan(ArrowHeadAngle) * head;
        var tip = ToPoint(s.End);
        var baseCenter = new Point(s.End.X - ux * head, s.End.Y - uy * head);
        var left = new Point(baseCenter.X - uy * halfWidth, baseCenter.Y + ux * halfWidth);
        var right = new Point(baseCenter.X + uy * halfWidth, baseCenter.Y - ux * halfWidth);

        dc.DrawLine(stroke, ToPoint(s.Start), baseCenter);
        var triangle = new StreamGeometry();
        using (var ctx = triangle.Open())
        {
            ctx.BeginFigure(tip, isFilled: true, isClosed: true);
            ctx.LineTo(left, isStroked: true, isSmoothJoin: false);
            ctx.LineTo(right, isStroked: true, isSmoothJoin: false);
        }
        triangle.Freeze();
        dc.DrawGeometry(brush, new Pen(brush, 1) { LineJoin = PenLineJoin.Round }, triangle);
    }

    private static void DrawShape(DrawingContext dc, ShapeAnnotation shape)
    {
        var stroke = new Pen(BrushOf(shape.Color), shape.Thickness) { LineJoin = PenLineJoin.Round };
        var r = new Rect(shape.Rect.X, shape.Rect.Y, shape.Rect.Width, shape.Rect.Height);
        if (shape.IsEllipse)
            dc.DrawEllipse(null, stroke, new Point(r.X + r.Width / 2, r.Y + r.Height / 2), r.Width / 2, r.Height / 2);
        else
            dc.DrawRectangle(null, stroke, r);
    }

    private static void DrawCensor(DrawingContext dc, CensorAnnotation censor, CensorPatchCache? patches)
    {
        var cover = CensorPatchCache.CoverPixels(censor.Rect);
        if (censor.Style == CensorStyle.Solid || patches is null)
        {
            // Whole-pixel aligned so no original pixel survives at the edges.
            dc.DrawRectangle(BrushOf(censor.Color with { A = 255 }), null, new Rect(cover.X, cover.Y, cover.Width, cover.Height));
            return;
        }

        if (patches.Get(censor) is { } patch)
            dc.DrawImage(patch.Patch, new Rect(patch.Area.X, patch.Area.Y, patch.Area.Width, patch.Area.Height));
    }

    private static Point ToPoint(PointD p) => new(p.X, p.Y);
}
