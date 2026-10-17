using System.Windows;
using System.Windows.Media;
using SScreentify.App.Rendering;
using SScreentify.App.Ui;
using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;

namespace SScreentify.App.Overlay;

/// <summary>Draws the session's annotations, the in-progress preview and the selected-annotation frame, clipped to the selection.</summary>
public sealed class AnnotationLayer : FrameworkElement
{
    public CaptureSession? Session { get; set; }
    public ToolController? Tools { get; set; }
    public CensorPatchCache? Patches { get; set; }
    public PixelRect MonitorBounds { get; set; }

    protected override void OnRender(DrawingContext dc)
    {
        if (Session is not { } session || session.Selection.Rect is not { IsEmpty: false } sel) return;

        var scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        dc.PushTransform(new MatrixTransform(1 / scale, 0, 0, 1 / scale, -MonitorBounds.X / scale, -MonitorBounds.Y / scale));
        dc.PushClip(new RectangleGeometry(new Rect(sel.X, sel.Y, sel.Width, sel.Height)));

        var preview = Tools?.Preview;
        var replaced = false;
        foreach (var item in session.Document.Items)
        {
            if (preview is not null && item.Id == preview.Id)
            {
                AnnotationRenderer.Draw(dc, preview, Patches);
                replaced = true;
            }
            else
            {
                AnnotationRenderer.Draw(dc, item, Patches);
            }
        }
        if (preview is not null && !replaced) AnnotationRenderer.Draw(dc, preview, Patches);
        dc.Pop();

        if (Tools?.SelectedBounds is { } b)
        {
            var frame = new Pen(Palette.SelectionBorder, 1.5 * scale) { DashStyle = new DashStyle(new[] { 3.0, 2.0 }, 0) };
            var r = new Rect(b.X, b.Y, b.Width, b.Height);
            r.Inflate(3 * scale, 3 * scale);
            dc.DrawRectangle(null, frame, r);
            dc.DrawEllipse(Palette.White, new Pen(Palette.SelectionBorder, 1.5 * scale), r.BottomRight, 4 * scale, 4 * scale);
        }
        dc.Pop();
    }
}
