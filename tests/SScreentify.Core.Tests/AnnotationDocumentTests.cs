using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;

namespace SScreentify.Core.Tests;

public class AnnotationDocumentTests
{
    private static readonly Rgba Red = Rgba.Parse("#F43F5E");

    private static ShapeAnnotation Rect(double x, double y, double w, double h) =>
        new(Guid.NewGuid(), Red, 4, new RectD(x, y, w, h), IsEllipse: false);

    [Fact]
    public void Rgba_ParseRoundTrip()
    {
        Assert.Equal("#F43F5E", Rgba.Parse("#F43F5E").ToHex());
        Assert.Equal(new Rgba(0x11, 0x22, 0x33, 0x80), Rgba.Parse("#80112233"));
        Assert.Throws<FormatException>(() => Rgba.Parse("zz"));
    }

    [Fact]
    public void AddUndoRedo()
    {
        var doc = new AnnotationDocument();
        var a = Rect(0, 0, 10, 10);
        var b = Rect(5, 5, 10, 10);
        doc.Add(a);
        doc.Add(b);
        Assert.True(doc.Undo());
        Assert.Equal(new Annotation[] { a }, doc.Items);
        Assert.True(doc.Redo());
        Assert.Equal(new Annotation[] { a, b }, doc.Items);
        Assert.False(doc.Redo());
    }

    [Fact]
    public void NewMutationClearsRedo()
    {
        var doc = new AnnotationDocument();
        doc.Add(Rect(0, 0, 1, 1));
        doc.Undo();
        doc.Add(Rect(1, 1, 1, 1));
        Assert.False(doc.CanRedo);
    }

    [Fact]
    public void ReplaceIsUndoable()
    {
        var doc = new AnnotationDocument();
        var a = Rect(0, 0, 10, 10);
        doc.Add(a);
        doc.Replace(a.Translate(10, 0));
        Assert.Equal(10, doc.Items[0].Bounds.X);
        doc.Undo();
        Assert.Equal(a, doc.Items[0]);
    }

    [Fact]
    public void Replace_UnknownId_Throws()
    {
        var doc = new AnnotationDocument();
        Assert.Throws<KeyNotFoundException>(() => doc.Replace(Rect(0, 0, 1, 1)));
    }

    [Fact]
    public void RemoveIsUndoable()
    {
        var doc = new AnnotationDocument();
        var a = Rect(0, 0, 10, 10);
        doc.Add(a);
        doc.Remove(a.Id);
        Assert.Empty(doc.Items);
        doc.Undo();
        Assert.Equal(new Annotation[] { a }, doc.Items);
    }

    [Fact]
    public void HitTest_ReturnsTopmost()
    {
        var doc = new AnnotationDocument();
        var censorBelow = new CensorAnnotation(Guid.NewGuid(), Red, new RectD(0, 0, 100, 100), CensorStyle.Solid);
        var censorAbove = new CensorAnnotation(Guid.NewGuid(), Red, new RectD(50, 50, 100, 100), CensorStyle.Pixelate);
        doc.Add(censorBelow);
        doc.Add(censorAbove);
        Assert.Equal(censorAbove, doc.HitTest(new PointD(75, 75), 4));
        Assert.Equal(censorBelow, doc.HitTest(new PointD(10, 10), 4));
        Assert.Null(doc.HitTest(new PointD(400, 400), 4));
    }

    [Fact]
    public void Segment_HitTestUsesTolerance()
    {
        var line = new SegmentAnnotation(Guid.NewGuid(), Red, 2, new PointD(0, 0), new PointD(100, 0), HasArrowHead: false);
        Assert.True(line.HitTest(new PointD(50, 3), 4));
        Assert.False(line.HitTest(new PointD(50, 6), 4));
        Assert.False(line.HitTest(new PointD(110, 0), 4));
    }

    [Fact]
    public void ShapeOutline_HitOnlyNearBorder()
    {
        var rect = Rect(0, 0, 100, 100);
        Assert.False(rect.HitTest(new PointD(50, 50), 4));
        Assert.True(rect.HitTest(new PointD(0, 50), 4));
        Assert.True(rect.HitTest(new PointD(50, 101), 4));

        var ellipse = new ShapeAnnotation(Guid.NewGuid(), Red, 4, new RectD(0, 0, 100, 100), IsEllipse: true);
        Assert.True(ellipse.HitTest(new PointD(50, 0), 4));
        Assert.False(ellipse.HitTest(new PointD(50, 50), 4));
        Assert.False(ellipse.HitTest(new PointD(2, 2), 4));

        var censor = new CensorAnnotation(Guid.NewGuid(), Red, new RectD(0, 0, 100, 100), CensorStyle.Blur);
        Assert.True(censor.HitTest(new PointD(50, 50), 4));
    }

    [Fact]
    public void Pen_ResizeTo_ScalesPoints()
    {
        var pen = new PenAnnotation(Guid.NewGuid(), Red, 4, new[] { new PointD(0, 0), new PointD(10, 20) }, IsMarker: false);
        var resized = (PenAnnotation)pen.ResizeTo(new RectD(100, 100, 20, 40));
        Assert.Equal(new[] { new PointD(100, 100), new PointD(120, 140) }, resized.Points);
        Assert.Equal(new RectD(100, 100, 20, 40), resized.Bounds);
    }

    [Fact]
    public void Text_TranslateMovesOriginAndBounds()
    {
        var text = new TextAnnotation(Guid.NewGuid(), Red, new PointD(10, 10), "hola", 20, new RectD(10, 10, 40, 24));
        var moved = (TextAnnotation)text.Translate(5, -5);
        Assert.Equal(new PointD(15, 5), moved.Origin);
        Assert.Equal(new RectD(15, 5, 40, 24), moved.Bounds);
        Assert.True(moved.HitTest(new PointD(20, 10), 0));
    }

    [Fact]
    public void Changed_FiresOnMutationAndUndo()
    {
        var doc = new AnnotationDocument();
        var count = 0;
        doc.Changed += () => count++;
        var a = Rect(0, 0, 1, 1);
        doc.Add(a);
        doc.Undo();
        doc.Redo();
        doc.Remove(a.Id);
        doc.Clear();
        Assert.Equal(5, count);
        Assert.False(doc.CanUndo);
    }
}
