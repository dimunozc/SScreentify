using System.Windows.Media.Imaging;
using SScreentify.App.Capture;
using SScreentify.App.Imaging;
using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;
using SScreentify.Core.Settings;

namespace SScreentify.App.Overlay;

public enum ToolKind { None, Select, Pen, Line, Arrow, Rectangle, Ellipse, Marker, Text, Censor }

/// <summary>One capture: the frozen desktop, its selection, annotations and current tool style.</summary>
public sealed class CaptureSession
{
    private ToolKind _tool;
    private Rgba _color;
    private double _thickness;
    private double _fontSize;
    private CensorStyle _censorStyle;

    public CaptureSession(CaptureResult capture, AppSettings settings)
    {
        Capture = capture;
        Image = capture.Pixels.ToBitmapSource();
        Selection = new SelectionModel(capture.VirtualBounds);
        _color = Rgba.Parse(settings.Color);
        _thickness = settings.Thickness;
        _fontSize = settings.FontSize;
        _censorStyle = settings.CensorStyle;
    }

    public CaptureResult Capture { get; }

    /// <summary>The frozen desktop as a 96-DPI bitmap: one image pixel per physical pixel.</summary>
    public BitmapSource Image { get; }

    public SelectionModel Selection { get; }
    public AnnotationDocument Document { get; } = new();

    public ToolKind Tool { get => _tool; set { if (_tool == value) return; _tool = value; ToolChanged?.Invoke(); } }
    public Rgba Color { get => _color; set { _color = value; StyleChanged?.Invoke(); } }
    public double Thickness { get => _thickness; set { _thickness = Math.Clamp(value, 1, 40); StyleChanged?.Invoke(); } }
    public double FontSize { get => _fontSize; set { _fontSize = Math.Clamp(value, 8, 96); StyleChanged?.Invoke(); } }
    public CensorStyle CensorStyle { get => _censorStyle; set { _censorStyle = value; StyleChanged?.Invoke(); } }

    public event Action? ToolChanged, StyleChanged;

    /// <summary>Carries the style the user picked back into settings.</summary>
    public AppSettings ApplyTo(AppSettings s) => s with
    {
        Color = Color.ToHex(),
        Thickness = Thickness,
        FontSize = FontSize,
        CensorStyle = CensorStyle,
        LastSelection = s.KeepSelection && Selection.IsUsable ? Selection.Rect : s.LastSelection,
    };
}
