using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SScreentify.App.Rendering;
using SScreentify.Core.Geometry;

namespace SScreentify.App.Overlay;

/// <summary>An in-place TextBox for the Text tool; commits as a TextAnnotation in capture coordinates.</summary>
public sealed class TextEditorHost
{
    private TextBox? _box;
    private OverlayWindow? _window;
    private PointD _origin;
    private double _fontSize;
    private Action<PointD, string, RectD>? _commit;

    public bool IsEditing => _box is not null;

    public void Begin(OverlayWindow window, PointD origin, CaptureSession session, Action<PointD, string, RectD> commit)
    {
        Commit();
        _window = window;
        _origin = origin;
        _fontSize = session.FontSize;
        _commit = commit;

        var c = session.Color;
        var scale = window.Scale;
        _box = new TextBox
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Foreground = AnnotationRenderer.BrushOf(c),
            CaretBrush = AnnotationRenderer.BrushOf(c),
            FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI"),
            FontWeight = FontWeights.SemiBold,
            FontSize = _fontSize / scale,
            AcceptsReturn = true,
            MinWidth = 24,
        };
        // TextBox content sits inside a small template margin; shift so typed text lines up with the committed text.
        Canvas.SetLeft(_box, (origin.X - window.Monitor.Bounds.X) / scale - 2);
        Canvas.SetTop(_box, (origin.Y - window.Monitor.Bounds.Y) / scale);
        window.EditorHost.Children.Add(_box);
        _box.Loaded += (_, _) => _box?.Focus();
    }

    public void Commit()
    {
        if (_box is null) return;
        var text = _box.Text;
        var measured = AnnotationRenderer.MeasureText(text, _fontSize);
        var commit = _commit;
        Remove();
        commit?.Invoke(_origin, text, new RectD(_origin.X, _origin.Y, measured.Width, measured.Height));
    }

    public void Cancel() => Remove();

    private void Remove()
    {
        if (_box is not null) _window?.EditorHost.Children.Remove(_box);
        _box = null;
        _window?.Focus();
        _window = null;
        _commit = null;
    }
}
