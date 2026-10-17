using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;

namespace SScreentify.App.Ui;

/// <summary>Own stroke icons on a 24×24 grid.</summary>
public static class Icons
{
    public static readonly Geometry Select = Parse("M6,4 L18,11 L13,12.5 L10.5,18 Z");
    public static readonly Geometry Pen = Parse("M4,20 L8,19 L19,8 L16,5 L5,16 Z");
    public static readonly Geometry Line = Parse("M5,19 L19,5");
    public static readonly Geometry Arrow = Parse("M5,19 L19,5 M9,5 L19,5 L19,15");
    public static readonly Geometry Rectangle = Parse("M4,6 H20 V18 H4 Z");
    public static readonly Geometry Ellipse = new EllipseGeometry(new Point(12, 12), 8, 8);
    public static readonly Geometry Marker = Parse("M4,20 H14 M7,15 L15,7 L19,11 L11,19 H7 Z");
    public static readonly Geometry Text = Parse("M6,6 H18 M12,6 V19 M9,19 H15");
    public static readonly Geometry Censor = Parse("M4,6 H20 V18 H4 Z M4,12 H20 M9.33,6 V18 M14.67,6 V18");
    public static readonly Geometry Undo = Parse("M9,14 L4,9 L9,4 M4,9 H15 A5,5 0 0 1 15,19 H12");
    public static readonly Geometry Redo = Parse("M15,14 L20,9 L15,4 M20,9 H9 A5,5 0 0 0 9,19 H12");
    public static readonly Geometry Save = Parse("M5,4 H16 L19,7 V20 H5 Z M8,4 V9 H15 V4 M8,20 V14 H16 V20");
    public static readonly Geometry Copy = Parse("M8,8 H20 V20 H8 Z M16,8 V5 A1,1 0 0 0 15,4 H5 A1,1 0 0 0 4,5 V15 A1,1 0 0 0 5,16 H8");
    public static readonly Geometry Print = Parse("M7,9 V4 H17 V9 M7,17 H4 V10 H20 V17 H17 M7,14 H17 V20 H7 Z");
    public static readonly Geometry Close = Parse("M6,6 L18,18 M18,6 L6,18");

    public static UIElement Create(Geometry geometry, double size = 17) => new Viewbox
    {
        Width = size,
        Height = size,
        Child = new Path
        {
            Data = geometry,
            Width = 24,
            Height = 24,
            StrokeThickness = 2.4,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
        }.WithStroke(),
    };

    private static Path WithStroke(this Path path)
    {
        path.SetBinding(Shape.StrokeProperty, new System.Windows.Data.Binding(nameof(Control.Foreground))
        {
            RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(Control), 1),
        });
        return path;
    }

    private static Geometry Parse(string data)
    {
        var g = Geometry.Parse(data);
        g.Freeze();
        return g;
    }
}
