using System.Windows.Input;
using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;

namespace SScreentify.App.Overlay;

/// <summary>Turns pointer and key input into annotations for the active drawing tool. Coordinates are physical capture pixels.</summary>
public sealed class ToolController
{
    private const double MinPenStep = 1.5;
    private const double MinShapeSize = 2;

    private readonly CaptureSession _session;
    private List<PointD>? _points;
    private PointD? _start;
    private Annotation? _dragOriginal;
    private PointD _dragStart;
    private bool _resizing;

    public ToolController(CaptureSession session)
    {
        _session = session;
        _session.Document.Changed += () =>
        {
            if (SelectedId is { } id && _session.Document.Items.All(a => a.Id != id)) SelectedId = null;
        };
        _session.ToolChanged += () => { SelectedId = null; Cancel(); };
        // Drawing tools only make sense on a selection; without one the user must be able to select again.
        _session.Selection.Changed += () =>
        {
            if (!_session.Selection.IsUsable && _session.Tool != ToolKind.None) _session.Tool = ToolKind.None;
        };
    }

    /// <summary>The annotation being drawn; when it shares an Id with a document item it replaces it while dragging.</summary>
    public Annotation? Preview { get; private set; }
    public Guid? SelectedId { get; private set; }

    public event Action? Changed;
    public event Action<PointD>? TextEditRequested;
    public event Action<string>? IndicatorRequested;

    public void OnMouseDown(PointD p, ModifierKeys m)
    {
        switch (_session.Tool)
        {
            case ToolKind.Pen or ToolKind.Marker:
                _points = new List<PointD> { p };
                Preview = NewPen();
                break;
            case ToolKind.Line or ToolKind.Arrow or ToolKind.Rectangle or ToolKind.Ellipse or ToolKind.Censor:
                _start = p;
                break;
            case ToolKind.Text:
                TextEditRequested?.Invoke(p);
                break;
            case ToolKind.Select:
                BeginSelectDrag(p);
                break;
        }
        Changed?.Invoke();
    }

    public void OnMouseMove(PointD p, ModifierKeys m)
    {
        if (_points is not null)
        {
            if (Distance(_points[^1], p) >= MinPenStep)
            {
                _points.Add(p);
                Preview = NewPen();
                Changed?.Invoke();
            }
            return;
        }

        if (_start is { } start)
        {
            Preview = BuildShape(start, Constrained(start, p, m), Preview?.Id ?? Guid.NewGuid());
            Changed?.Invoke();
            return;
        }

        if (_dragOriginal is { } original)
        {
            Preview = _resizing
                ? original.ResizeTo(RectD.FromPoints(new PointD(original.Bounds.X, original.Bounds.Y), p))
                : original.Translate(p.X - _dragStart.X, p.Y - _dragStart.Y);
            Changed?.Invoke();
        }
    }

    public void OnMouseUp(PointD p, ModifierKeys m)
    {
        if (_points is not null)
        {
            if (Distance(_points[^1], p) >= MinPenStep) _points.Add(p);
            if (_points.Count >= 2) _session.Document.Add(NewPen());
        }
        else if (_start is { } start)
        {
            var shape = BuildShape(start, Constrained(start, p, m), Guid.NewGuid());
            if (IsBigEnough(shape)) _session.Document.Add(shape);
        }
        else if (_dragOriginal is not null && Preview is not null && Preview != _dragOriginal)
        {
            _session.Document.Replace(Preview);
        }
        Cancel();
        Changed?.Invoke();
    }

    public void OnWheel(int delta)
    {
        var step = Math.Sign(delta);
        if (step == 0) return;
        if (_session.Tool == ToolKind.Text)
        {
            _session.FontSize += 2 * step;
            IndicatorRequested?.Invoke($"{_session.FontSize:0} pt");
        }
        else
        {
            _session.Thickness += step;
            IndicatorRequested?.Invoke($"{_session.Thickness:0} px");
        }
    }

    public bool OnKey(Key key, ModifierKeys m)
    {
        var ctrl = m.HasFlag(ModifierKeys.Control);
        var shift = m.HasFlag(ModifierKeys.Shift);
        switch (key)
        {
            case Key.V when !ctrl && _session.Selection.IsUsable:
                _session.Tool = ToolKind.Select;
                return true;
            case Key.Delete when SelectedId is { } id:
                _session.Document.Remove(id);
                SelectedId = null;
                Changed?.Invoke();
                return true;
            case Key.Z when ctrl && shift:
            case Key.Y when ctrl:
                _session.Document.Redo();
                return true;
            case Key.Z when ctrl:
                _session.Document.Undo();
                return true;
            default:
                return false;
        }
    }

    /// <summary>Adds the text typed in the editor; blank text adds nothing.</summary>
    public void CommitText(PointD origin, string text, RectD bounds)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        _session.Document.Add(new TextAnnotation(Guid.NewGuid(), _session.Color, origin, text, _session.FontSize, bounds));
    }

    /// <summary>Bounds of the selected annotation (with any in-progress drag applied), for drawing the selection frame.</summary>
    public RectD? SelectedBounds =>
        SelectedId is { } id ? (Preview?.Id == id ? Preview : _session.Document.Items.FirstOrDefault(a => a.Id == id))?.Bounds : null;

    private void BeginSelectDrag(PointD p)
    {
        var tolerance = Math.Max(4, _session.Thickness);
        if (SelectedId is { } id && _session.Document.Items.FirstOrDefault(a => a.Id == id) is { } selected
            && IsNearCorner(selected.Bounds, p, Math.Max(6, tolerance)))
        {
            _dragOriginal = selected;
            _resizing = true;
            Preview = selected;
            return;
        }

        var hit = _session.Document.HitTest(p, tolerance);
        SelectedId = hit?.Id;
        if (hit is null) return;
        _dragOriginal = hit;
        _resizing = false;
        _dragStart = p;
        Preview = hit;
    }

    /// <summary>Only the bottom-right corner resizes; the top-left stays anchored.</summary>
    private static bool IsNearCorner(RectD b, PointD p, double tolerance) =>
        Math.Abs(p.X - b.Right) <= tolerance && Math.Abs(p.Y - b.Bottom) <= tolerance;

    private Annotation BuildShape(PointD start, PointD end, Guid id) => _session.Tool switch
    {
        ToolKind.Line => new SegmentAnnotation(id, _session.Color, _session.Thickness, start, end, HasArrowHead: false),
        ToolKind.Arrow => new SegmentAnnotation(id, _session.Color, _session.Thickness, start, end, HasArrowHead: true),
        ToolKind.Rectangle => new ShapeAnnotation(id, _session.Color, _session.Thickness, RectD.FromPoints(start, end), IsEllipse: false),
        ToolKind.Ellipse => new ShapeAnnotation(id, _session.Color, _session.Thickness, RectD.FromPoints(start, end), IsEllipse: true),
        ToolKind.Censor => new CensorAnnotation(id, _session.Color, RectD.FromPoints(start, end), _session.CensorStyle),
        _ => throw new InvalidOperationException($"La herramienta {_session.Tool} no dibuja formas."),
    };

    private PointD Constrained(PointD start, PointD p, ModifierKeys m)
    {
        if (!m.HasFlag(ModifierKeys.Shift)) return p;
        return _session.Tool is ToolKind.Line or ToolKind.Arrow ? Constrain.SnapAngle45(start, p) : Constrain.SquareCorner(start, p);
    }

    private static bool IsBigEnough(Annotation a) => a is SegmentAnnotation s
        ? Distance(s.Start, s.End) >= MinShapeSize
        : a.Bounds.Width >= MinShapeSize && a.Bounds.Height >= MinShapeSize;

    private PenAnnotation NewPen() =>
        new(Preview?.Id ?? Guid.NewGuid(), _session.Color, _session.Thickness, _points!.ToArray(), IsMarker: _session.Tool == ToolKind.Marker);

    private void Cancel()
    {
        _points = null;
        _start = null;
        _dragOriginal = null;
        _resizing = false;
        Preview = null;
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
