using SScreentify.Core.Geometry;

namespace SScreentify.Core.Tests;

public class SelectionModelTests
{
    private static readonly PixelRect Screen = new(0, 0, 1920, 1080);

    [Fact]
    public void Create_NormalizesWhenDraggingUpLeft()
    {
        var model = new SelectionModel(Screen);
        model.BeginCreate(new PixelPoint(100, 100));
        model.UpdateCreate(new PixelPoint(40, 30));
        Assert.Equal(new PixelRect(40, 30, 60, 70), model.Rect);
    }

    [Fact]
    public void Create_ClampsToBounds()
    {
        var model = new SelectionModel(new PixelRect(-1920, 0, 3840, 1080));
        model.BeginCreate(new PixelPoint(0, 500));
        model.UpdateCreate(new PixelPoint(5000, -50));
        Assert.Equal(new PixelRect(0, 0, 1920, 500), model.Rect);
        Assert.Equal(1920, model.Rect!.Value.Right);
    }

    [Fact]
    public void HitTest_PrefersHandleOverMove()
    {
        var model = new SelectionModel(Screen);
        model.Set(new PixelRect(100, 100, 200, 100));
        Assert.Equal(Handle.TopLeft, model.HitTest(new PixelPoint(100, 100), 6));
        Assert.Equal(Handle.Top, model.HitTest(new PixelPoint(200, 100), 6));
        Assert.Equal(Handle.Right, model.HitTest(new PixelPoint(300, 150), 6));
        Assert.Equal(Handle.BottomRight, model.HitTest(new PixelPoint(298, 202), 6));
        Assert.Equal(Handle.Move, model.HitTest(new PixelPoint(150, 150), 6));
        Assert.Equal(Handle.None, model.HitTest(new PixelPoint(10, 10), 6));
    }

    [Fact]
    public void HitTest_WithoutSelection_ReturnsNone()
    {
        var model = new SelectionModel(Screen);
        Assert.Equal(Handle.None, model.HitTest(new PixelPoint(10, 10), 6));
    }

    [Fact]
    public void DragRightHandlePastLeftEdge_Flips()
    {
        var model = new SelectionModel(Screen);
        model.Set(new PixelRect(100, 100, 200, 100));
        model.BeginDrag(Handle.Right, new PixelPoint(300, 150));
        model.UpdateDrag(new PixelPoint(50, 150));
        model.EndDrag();
        Assert.Equal(new PixelRect(50, 100, 50, 100), model.Rect);
    }

    [Fact]
    public void Move_StaysInsideBounds()
    {
        var model = new SelectionModel(new PixelRect(0, 0, 1000, 1000));
        model.Set(new PixelRect(900, 900, 50, 50));
        model.BeginDrag(Handle.Move, new PixelPoint(920, 920));
        model.UpdateDrag(new PixelPoint(1120, 1120));
        model.EndDrag();
        Assert.Equal(new PixelRect(950, 950, 50, 50), model.Rect);
    }

    [Fact]
    public void Nudge_MovesAndResizes()
    {
        var model = new SelectionModel(Screen);
        model.Set(new PixelRect(10, 10, 20, 20));
        model.Nudge(1, 0, resize: false);
        Assert.Equal(new PixelRect(11, 10, 20, 20), model.Rect);
        model.Nudge(0, 1, resize: true);
        Assert.Equal(new PixelRect(11, 10, 20, 21), model.Rect);
    }

    [Fact]
    public void Nudge_DoesNotLeaveBounds()
    {
        var model = new SelectionModel(Screen);
        model.Set(new PixelRect(0, 0, 20, 20));
        model.Nudge(-1, -1, resize: false);
        Assert.Equal(new PixelRect(0, 0, 20, 20), model.Rect);
    }

    [Fact]
    public void IsUsable_FalseForClick()
    {
        var model = new SelectionModel(Screen);
        model.BeginCreate(new PixelPoint(10, 10));
        model.UpdateCreate(new PixelPoint(11, 10));
        Assert.False(model.IsUsable);
        model.UpdateCreate(new PixelPoint(12, 12));
        Assert.True(model.IsUsable);
    }

    [Fact]
    public void Changed_FiresOnEveryMutation()
    {
        var model = new SelectionModel(Screen);
        var count = 0;
        model.Changed += () => count++;
        model.BeginCreate(new PixelPoint(10, 10));
        model.UpdateCreate(new PixelPoint(50, 50));
        model.BeginDrag(Handle.Move, new PixelPoint(20, 20));
        model.UpdateDrag(new PixelPoint(25, 25));
        model.EndDrag();
        model.Nudge(1, 0, resize: false);
        model.Set(new PixelRect(0, 0, 5, 5));
        model.Clear();
        Assert.Equal(6, count);
        Assert.Null(model.Rect);
    }
}
