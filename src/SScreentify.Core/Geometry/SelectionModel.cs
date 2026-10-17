namespace SScreentify.Core.Geometry;

public enum Handle { None, Move, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left }

/// <summary>The capture region, always clamped to the virtual desktop bounds.</summary>
public sealed class SelectionModel
{
    private readonly PixelRect _bounds;
    private PixelPoint _createAnchor;
    private Handle _dragHandle;
    private PixelPoint _dragStart;
    private PixelRect _dragOrigin;

    public SelectionModel(PixelRect bounds) => _bounds = bounds;

    public PixelRect? Rect { get; private set; }

    public bool IsUsable => Rect is { Width: >= 2, Height: >= 2 };

    public event Action? Changed;

    public void BeginCreate(PixelPoint p)
    {
        _createAnchor = ClampPoint(p);
        Update(new PixelRect(_createAnchor.X, _createAnchor.Y, 0, 0));
    }

    public void UpdateCreate(PixelPoint p) => Update(PixelRect.FromPoints(_createAnchor, ClampPoint(p)));

    public Handle HitTest(PixelPoint p, int handleRadius)
    {
        if (Rect is not { } r)
            return Handle.None;

        bool Near(int x, int y) => Math.Abs(p.X - x) <= handleRadius && Math.Abs(p.Y - y) <= handleRadius;
        int midX = r.X + r.Width / 2, midY = r.Y + r.Height / 2;

        if (Near(r.X, r.Y)) return Handle.TopLeft;
        if (Near(r.Right, r.Y)) return Handle.TopRight;
        if (Near(r.Right, r.Bottom)) return Handle.BottomRight;
        if (Near(r.X, r.Bottom)) return Handle.BottomLeft;
        if (Near(midX, r.Y)) return Handle.Top;
        if (Near(r.Right, midY)) return Handle.Right;
        if (Near(midX, r.Bottom)) return Handle.Bottom;
        if (Near(r.X, midY)) return Handle.Left;
        return r.Contains(p) ? Handle.Move : Handle.None;
    }

    public void BeginDrag(Handle h, PixelPoint p)
    {
        if (Rect is not { } r || h == Handle.None)
            return;
        _dragHandle = h;
        _dragStart = p;
        _dragOrigin = r;
    }

    public void UpdateDrag(PixelPoint p)
    {
        if (_dragHandle == Handle.None)
            return;

        int dx = p.X - _dragStart.X, dy = p.Y - _dragStart.Y;
        var o = _dragOrigin;

        if (_dragHandle == Handle.Move)
        {
            Update(Translate(o, dx, dy));
            return;
        }

        int left = o.X, top = o.Y, right = o.Right, bottom = o.Bottom;
        if (_dragHandle is Handle.TopLeft or Handle.Left or Handle.BottomLeft) left += dx;
        if (_dragHandle is Handle.TopRight or Handle.Right or Handle.BottomRight) right += dx;
        if (_dragHandle is Handle.TopLeft or Handle.Top or Handle.TopRight) top += dy;
        if (_dragHandle is Handle.BottomLeft or Handle.Bottom or Handle.BottomRight) bottom += dy;

        Update(PixelRect.FromPoints(ClampPoint(new PixelPoint(left, top)), ClampPoint(new PixelPoint(right, bottom))));
    }

    public void EndDrag() => _dragHandle = Handle.None;

    public void Nudge(int dx, int dy, bool resize)
    {
        if (Rect is not { } r)
            return;

        if (!resize)
        {
            Update(Translate(r, dx, dy));
            return;
        }

        var corner = ClampPoint(new PixelPoint(r.Right + dx, r.Bottom + dy));
        Update(new PixelRect(r.X, r.Y, Math.Max(0, corner.X - r.X), Math.Max(0, corner.Y - r.Y)));
    }

    public void Set(PixelRect r) => Update(_bounds.Intersect(r));

    public void Clear()
    {
        _dragHandle = Handle.None;
        Rect = null;
        Changed?.Invoke();
    }

    private PixelRect Translate(PixelRect r, int dx, int dy)
    {
        var x = Math.Clamp(r.X + dx, _bounds.X, _bounds.Right - r.Width);
        var y = Math.Clamp(r.Y + dy, _bounds.Y, _bounds.Bottom - r.Height);
        return r with { X = x, Y = y };
    }

    private PixelPoint ClampPoint(PixelPoint p) =>
        new(Math.Clamp(p.X, _bounds.X, _bounds.Right), Math.Clamp(p.Y, _bounds.Y, _bounds.Bottom));

    private void Update(PixelRect r)
    {
        Rect = r;
        Changed?.Invoke();
    }
}
