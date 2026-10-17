using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SScreentify.App.Ui;

/// <summary>
/// Acrylic panel: the frozen capture under the panel, blurred, behind a translucent dark fill.
/// The capture is static, so the "acrylic" is just a blurred crop of it.
/// </summary>
public sealed class GlassPanel : Border
{
    private const double Radius = 12;
    private readonly Rectangle _backdrop = new() { Effect = new BlurEffect { Radius = 24, KernelType = KernelType.Gaussian } };
    private readonly Border _frame = new()
    {
        CornerRadius = new CornerRadius(Radius),
        BorderThickness = new Thickness(1),
        Padding = new Thickness(5),
    };

    public GlassPanel()
    {
        _frame.SetResourceReference(BackgroundProperty, "PanelBrush");
        _frame.SetResourceReference(BorderBrushProperty, "PanelBorderBrush");
        var layers = new Grid();
        layers.Children.Add(_backdrop);
        layers.Children.Add(_frame);
        Child = layers;
        Effect = new DropShadowEffect { BlurRadius = 30, Opacity = 0.4, ShadowDepth = 6, Direction = 270 };
        SizeChanged += (_, e) => layers.Clip = new RectangleGeometry(new Rect(e.NewSize), Radius, Radius);
    }

    public UIElement? Content
    {
        get => _frame.Child;
        set => _frame.Child = value;
    }

    public bool HasBackdrop => _backdrop.Fill is not null;

    /// <summary>Shows <paramref name="viewbox"/> (pixels of <paramref name="backdrop"/>) blurred behind the panel.</summary>
    public void SetBackdrop(BitmapSource? backdrop, Rect viewbox)
    {
        _backdrop.Fill = backdrop is null ? null : new ImageBrush(backdrop)
        {
            ViewboxUnits = BrushMappingMode.Absolute,
            Viewbox = viewbox,
            Stretch = Stretch.Fill,
        };
    }
}
