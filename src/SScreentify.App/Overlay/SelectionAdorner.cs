using System.Globalization;
using System.Windows;
using System.Windows.Media;
using SScreentify.App.Ui;
using SScreentify.Core.Geometry;

namespace SScreentify.App.Overlay;

/// <summary>Draws the veil outside the selection, its border, handles and size pill.</summary>
public sealed class SelectionAdorner : FrameworkElement
{
    public const double HandleRadiusDip = 4;
    public const double BorderDip = 1.5;

    public CaptureSession? Session { get; set; }
    public PixelRect MonitorBounds { get; set; }

    protected override void OnRender(DrawingContext dc)
    {
        if (Session is null) return;

        var scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        var virtualRect = ToDip(Session.Capture.VirtualBounds, scale);
        var selection = Session.Selection.Rect;

        if (selection is not { IsEmpty: false } sel)
        {
            dc.DrawRectangle(Palette.Veil, null, virtualRect);
            return;
        }

        var selRect = ToDip(sel, scale);
        dc.DrawGeometry(Palette.Veil, null,
            new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(virtualRect), new RectangleGeometry(selRect)));

        var border = new Pen(Palette.SelectionBorder, BorderDip);
        dc.DrawRectangle(null, border, selRect);

        if (Session.Tool == ToolKind.None)
            foreach (var p in HandlePoints(selRect))
                dc.DrawEllipse(Palette.White, border, p, HandleRadiusDip, HandleRadiusDip);

        DrawSizePill(dc, sel, selRect);
    }

    private void DrawSizePill(DrawingContext dc, PixelRect sel, Rect selRect)
    {
        var text = new FormattedText($"{sel.Width} × {sel.Height}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(Palette.Font, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal), 12, Palette.Text,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        var pill = new Size(text.Width + 16, text.Height + 6);
        var above = ToolbarPlacement.SizeLabelFitsAbove(sel, pill.Height + 6, Session!.Capture.Monitors);
        var y = above ? selRect.Top - pill.Height - 6 : selRect.Top + 6;
        var box = new Rect(new Point(selRect.Left, y), pill);
        dc.DrawRoundedRectangle(Palette.Pill, null, box, 6, 6);
        dc.DrawText(text, new Point(box.Left + 8, box.Top + 3));
    }

    private static IEnumerable<Point> HandlePoints(Rect r)
    {
        double cx = r.Left + r.Width / 2, cy = r.Top + r.Height / 2;
        yield return r.TopLeft; yield return new Point(cx, r.Top); yield return r.TopRight; yield return new Point(r.Right, cy);
        yield return r.BottomRight; yield return new Point(cx, r.Bottom); yield return r.BottomLeft; yield return new Point(r.Left, cy);
    }

    private Rect ToDip(PixelRect r, double scale) =>
        new((r.X - MonitorBounds.X) / scale, (r.Y - MonitorBounds.Y) / scale, r.Width / scale, r.Height / scale);
}
