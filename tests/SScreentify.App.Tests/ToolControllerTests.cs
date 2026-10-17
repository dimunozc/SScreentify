using System.Windows.Input;
using SScreentify.App.Overlay;
using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;

namespace SScreentify.App.Tests;

public class ToolControllerTests
{
    private static (CaptureSession Session, ToolController Tools) Setup(ToolKind tool)
    {
        var session = ToolbarTests.NewSession(200, 200);
        session.Tool = tool;
        return (session, new ToolController(session));
    }

    private static void Drag(ToolController tools, PointD from, PointD to, ModifierKeys m = ModifierKeys.None)
    {
        tools.OnMouseDown(from, m);
        tools.OnMouseMove(new PointD((from.X + to.X) / 2, (from.Y + to.Y) / 2), m);
        tools.OnMouseMove(to, m);
        tools.OnMouseUp(to, m);
    }

    [Fact]
    public void Rectangle_DragAddsOneShapeWithSessionStyle()
    {
        var (session, tools) = Setup(ToolKind.Rectangle);
        session.Thickness = 6;
        Drag(tools, new PointD(50, 40), new PointD(10, 10));
        var shape = Assert.IsType<ShapeAnnotation>(Assert.Single(session.Document.Items));
        Assert.Equal(new RectD(10, 10, 40, 30), shape.Rect);
        Assert.Equal(6, shape.Thickness);
        Assert.Equal(session.Color, shape.Color);
        Assert.Null(tools.Preview);
    }

    [Fact]
    public void Ellipse_ShiftMakesCircle()
    {
        var (session, tools) = Setup(ToolKind.Ellipse);
        Drag(tools, new PointD(10, 10), new PointD(50, 20), ModifierKeys.Shift);
        var shape = Assert.IsType<ShapeAnnotation>(Assert.Single(session.Document.Items));
        Assert.True(shape.IsEllipse);
        Assert.Equal(shape.Rect.Width, shape.Rect.Height);
    }

    [Fact]
    public void Line_ShiftSnapsTo45()
    {
        var (session, tools) = Setup(ToolKind.Line);
        Drag(tools, new PointD(0, 0), new PointD(100, 8), ModifierKeys.Shift);
        var line = Assert.IsType<SegmentAnnotation>(Assert.Single(session.Document.Items));
        Assert.False(line.HasArrowHead);
        Assert.Equal(0, line.End.Y, 6);
    }

    [Fact]
    public void Pen_RecordsPointsAndIgnoresJitter()
    {
        var (session, tools) = Setup(ToolKind.Pen);
        tools.OnMouseDown(new PointD(0, 0), ModifierKeys.None);
        tools.OnMouseMove(new PointD(0.5, 0.5), ModifierKeys.None);
        tools.OnMouseMove(new PointD(10, 0), ModifierKeys.None);
        tools.OnMouseUp(new PointD(20, 0), ModifierKeys.None);
        var pen = Assert.IsType<PenAnnotation>(Assert.Single(session.Document.Items));
        Assert.Equal(new[] { new PointD(0, 0), new PointD(10, 0), new PointD(20, 0) }, pen.Points);
    }

    [Fact]
    public void ClickWithoutDrag_AddsNothing()
    {
        var (session, tools) = Setup(ToolKind.Arrow);
        tools.OnMouseDown(new PointD(10, 10), ModifierKeys.None);
        tools.OnMouseUp(new PointD(10, 10), ModifierKeys.None);
        Assert.Empty(session.Document.Items);
    }

    [Fact]
    public void Wheel_ChangesThicknessOrFontSize()
    {
        var (session, tools) = Setup(ToolKind.Pen);
        var start = session.Thickness;
        tools.OnWheel(120);
        Assert.Equal(start + 1, session.Thickness);
        session.Tool = ToolKind.Text;
        var font = session.FontSize;
        tools.OnWheel(-120);
        Assert.Equal(font - 2, session.FontSize);
    }

    [Fact]
    public void SelectTool_MovesAndDeletesWithUndo()
    {
        var (session, tools) = Setup(ToolKind.Rectangle);
        Drag(tools, new PointD(10, 10), new PointD(50, 50));
        session.Tool = ToolKind.Select;

        Drag(tools, new PointD(10, 30), new PointD(20, 40));
        var moved = Assert.IsType<ShapeAnnotation>(Assert.Single(session.Document.Items));
        Assert.Equal(new RectD(20, 20, 40, 40), moved.Rect);
        Assert.Equal(moved.Id, tools.SelectedId);

        Assert.True(tools.OnKey(Key.Delete, ModifierKeys.None));
        Assert.Empty(session.Document.Items);
        Assert.True(tools.OnKey(Key.Z, ModifierKeys.Control));
        Assert.Single(session.Document.Items);
        Assert.True(tools.OnKey(Key.Z, ModifierKeys.Control));
        Assert.Equal(new RectD(10, 10, 40, 40), ((ShapeAnnotation)session.Document.Items[0]).Rect);
        Assert.True(tools.OnKey(Key.Y, ModifierKeys.Control));
        Assert.Equal(new RectD(20, 20, 40, 40), ((ShapeAnnotation)session.Document.Items[0]).Rect);
        Assert.True(tools.OnKey(Key.Z, ModifierKeys.Control));
        Assert.True(tools.OnKey(Key.Z, ModifierKeys.Control | ModifierKeys.Shift));
        Assert.Equal(new RectD(20, 20, 40, 40), ((ShapeAnnotation)session.Document.Items[0]).Rect);
    }

    [Fact]
    public void SelectTool_CornerHandleResizes()
    {
        var (session, tools) = Setup(ToolKind.Rectangle);
        Drag(tools, new PointD(10, 10), new PointD(50, 50));
        session.Tool = ToolKind.Select;
        tools.OnMouseDown(new PointD(10, 30), ModifierKeys.None);
        tools.OnMouseUp(new PointD(10, 30), ModifierKeys.None);

        Drag(tools, new PointD(50, 50), new PointD(70, 90));
        Assert.Equal(new RectD(10, 10, 60, 80), ((ShapeAnnotation)session.Document.Items[0]).Bounds);
    }

    [Fact]
    public void VKey_SwitchesToSelect()
    {
        var (session, tools) = Setup(ToolKind.Pen);
        session.Selection.Set(new PixelRect(0, 0, 50, 50));
        Assert.True(tools.OnKey(Key.V, ModifierKeys.None));
        Assert.Equal(ToolKind.Select, session.Tool);
    }

    [Fact]
    public void TextTool_RequestsEditorAndCommitsText()
    {
        var (session, tools) = Setup(ToolKind.Text);
        PointD? requested = null;
        tools.TextEditRequested += p => requested = p;
        tools.OnMouseDown(new PointD(30, 40), ModifierKeys.None);
        tools.OnMouseUp(new PointD(30, 40), ModifierKeys.None);
        Assert.Equal(new PointD(30, 40), requested);

        tools.CommitText(new PointD(30, 40), "hola", new RectD(30, 40, 50, 25));
        var text = Assert.IsType<TextAnnotation>(Assert.Single(session.Document.Items));
        Assert.Equal("hola", text.Text);
        Assert.Equal(session.FontSize, text.FontSize);

        tools.CommitText(new PointD(0, 0), "   ", new RectD(0, 0, 1, 1));
        Assert.Single(session.Document.Items);
    }
}
