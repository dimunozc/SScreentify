using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SScreentify.App.Imaging;
using SScreentify.App.Rendering;
using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;
using SScreentify.Core.Imaging;

namespace SScreentify.App.Tests;

public class AnnotationRendererTests
{
    private static readonly Rgba Red = Rgba.Parse("#F43F5E");

    private static PixelBuffer Render(int w, int h, params Annotation[] items)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            foreach (var a in items) AnnotationRenderer.Draw(dc, a, patches: null);
        }
        var bitmap = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        return bitmap.ToPixelBuffer();
    }

    private static void AssertNear(Rgba expected, Rgba actual)
    {
        Assert.InRange(actual.R, expected.R - 8, expected.R + 8);
        Assert.InRange(actual.G, expected.G - 8, expected.G + 8);
        Assert.InRange(actual.B, expected.B - 8, expected.B + 8);
        Assert.InRange(actual.A, 247, 255);
    }

    [Fact]
    public void Rectangle_DrawsColorOnBorderNotInside()
    {
        StaRunner.Run(() =>
        {
            var image = Render(64, 64, new ShapeAnnotation(Guid.NewGuid(), Red, 4, new RectD(10, 10, 40, 40), IsEllipse: false));
            AssertNear(Red, image.GetPixel(10, 30));
            Assert.Equal(0, image.GetPixel(30, 30).A);
        });
    }

    [Fact]
    public void Arrow_HeadIsFilledNearEnd()
    {
        StaRunner.Run(() =>
        {
            var image = Render(100, 100, new SegmentAnnotation(Guid.NewGuid(), Red, 3, new PointD(10, 50), new PointD(90, 50), HasArrowHead: true));
            AssertNear(Red, image.GetPixel(82, 52)); // inside the head, outside the 3 px shaft
            AssertNear(Red, image.GetPixel(50, 50));
            Assert.Equal(0, image.GetPixel(50, 54).A);
        });
    }

    [Fact]
    public void Marker_IsTranslucent()
    {
        StaRunner.Run(() =>
        {
            var image = Render(64, 64, new PenAnnotation(Guid.NewGuid(), Red, 4, new[] { new PointD(5, 32), new PointD(60, 32) }, IsMarker: true));
            Assert.InRange(image.GetPixel(30, 32).A, 90, 115);
        });
    }

    [Fact]
    public void Text_DrawsInsideMeasuredBounds()
    {
        StaRunner.Run(() =>
        {
            var size = AnnotationRenderer.MeasureText("Hola", 20);
            var text = new TextAnnotation(Guid.NewGuid(), Red, new PointD(5, 5), "Hola", 20, new RectD(5, 5, size.Width, size.Height));
            var image = Render(100, 60, text);
            var painted = 0;
            for (var y = 0; y < 60; y++)
            for (var x = 0; x < 100; x++)
                if (image.GetPixel(x, y).A > 0)
                {
                    painted++;
                    Assert.True(text.Bounds.Inflate(1).Contains(new PointD(x, y)), $"pixel {x},{y} outside bounds");
                }
            Assert.True(painted > 20);
        });
    }

    [Fact]
    public void MeasureText_GrowsWithFontSize()
    {
        var small = AnnotationRenderer.MeasureText("Hola", 12);
        var big = AnnotationRenderer.MeasureText("Hola", 36);
        Assert.True(big.Width > small.Width * 2);
        Assert.True(big.Height > small.Height * 2);
        Assert.True(AnnotationRenderer.MeasureText("a\nb", 20).Height > AnnotationRenderer.MeasureText("a", 20).Height * 1.5);
    }
}
