using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SScreentify.App.Imaging;
using SScreentify.App.Rendering;
using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;
using SScreentify.Core.Imaging;

namespace SScreentify.App.Export;

/// <summary>Produces the final image: the selection crop with annotations burned in, one output pixel per physical pixel.</summary>
public static class Flattener
{
    /// <param name="captureOrigin">Capture-coordinate position of the capture buffer's top-left (the virtual desktop origin).</param>
    /// <param name="selection">Region to export, in capture coordinates; must be at least 2×2.</param>
    public static PixelBuffer Flatten(PixelBuffer capture, PixelRect captureOrigin, PixelRect selection, IReadOnlyList<Annotation> items)
    {
        if (selection.Width < 2 || selection.Height < 2)
            throw new ArgumentException("La selección es demasiado pequeña para exportar.", nameof(selection));

        var local = new PixelRect(selection.X - captureOrigin.X, selection.Y - captureOrigin.Y, selection.Width, selection.Height);
        var crop = capture.Crop(local);

        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.NearestNeighbor);
        var patches = new CensorPatchCache(capture, captureOrigin);
        using (var dc = visual.RenderOpen())
        {
            dc.DrawImage(crop.ToBitmapSource(), new Rect(0, 0, crop.Width, crop.Height));
            dc.PushTransform(new TranslateTransform(-selection.X, -selection.Y));
            foreach (var item in items) AnnotationRenderer.Draw(dc, item, patches);
            dc.Pop();
        }

        var bitmap = new RenderTargetBitmap(crop.Width, crop.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap.ToPixelBuffer();
    }
}
