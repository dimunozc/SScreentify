using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SScreentify.Core.Annotations;
using Forms = System.Windows.Forms;

namespace SScreentify.App.Ui;

/// <summary>Eight preset colors plus a custom picker.</summary>
public sealed class ColorPalette : UserControl
{
    public static readonly IReadOnlyList<string> Presets =
        new[] { "#F43F5E", "#F97316", "#FACC15", "#22C55E", "#3B82F6", "#A855F7", "#FFFFFF", "#111827" };

    public ColorPalette()
    {
        var grid = new System.Windows.Controls.Primitives.UniformGrid { Columns = 4 };
        foreach (var hex in Presets)
        {
            var c = Rgba.Parse(hex);
            var swatch = new Border
            {
                Width = 18, Height = 18, CornerRadius = new CornerRadius(9),
                Background = new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 255, 255, 255)), BorderThickness = new Thickness(1),
            };
            grid.Children.Add(ToolsBar.Button(swatch, Texts, () => hex, () => ColorChosen?.Invoke(c)));
        }

        var custom = new Button { Margin = new Thickness(1, 4, 1, 0), Height = 26, Width = double.NaN };
        Texts.Add(() => custom.Content = Strings.PaletteCustom);
        custom.SetResourceReference(StyleProperty, "GlassButton");
        custom.Width = double.NaN;
        custom.SetResourceReference(Control.FontFamilyProperty, "UiFont");
        custom.Click += (_, _) => PickCustom();

        var stack = new StackPanel();
        stack.Children.Add(grid);
        stack.Children.Add(custom);
        Content = new GlassPanel { Content = stack };
    }

    public LocalizedTexts Texts { get; } = new();

    public event Action<Rgba>? ColorChosen;

    private void PickCustom()
    {
        using var dialog = new Forms.ColorDialog { FullOpen = true, AnyColor = true };
        if (dialog.ShowDialog() == Forms.DialogResult.OK)
            ColorChosen?.Invoke(new Rgba(dialog.Color.R, dialog.Color.G, dialog.Color.B));
    }
}
