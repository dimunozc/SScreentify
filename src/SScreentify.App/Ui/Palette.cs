using System.Windows.Media;

namespace SScreentify.App.Ui;

/// <summary>Cristal oscuro tokens (spec §5) for code-drawn visuals; Theme.xaml mirrors them for XAML.</summary>
public static class Palette
{
    public static readonly Brush Veil = Freeze(new SolidColorBrush(Color.FromArgb(0x8C, 10, 12, 20)));        // 0.55
    public static readonly Brush Accent = Freeze(new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)));
    public static readonly Brush SelectionBorder = Freeze(new SolidColorBrush(Color.FromRgb(0x60, 0xA5, 0xFA)));
    public static readonly Brush Panel = Freeze(new SolidColorBrush(Color.FromArgb(0xC7, 28, 32, 44)));        // 0.78
    public static readonly Brush PanelBorder = Freeze(new SolidColorBrush(Color.FromArgb(0x14, 255, 255, 255))); // 8 %
    public static readonly Brush Pill = Freeze(new SolidColorBrush(Color.FromArgb(0xCC, 20, 24, 34)));
    public static readonly Brush Icon = Freeze(new SolidColorBrush(Color.FromRgb(0xD1, 0xD5, 0xDB)));
    public static readonly Brush Text = Freeze(new SolidColorBrush(Color.FromRgb(0xE5, 0xE7, 0xEB)));
    public static readonly Brush White = Brushes.White;

    public static readonly FontFamily Font = new("Segoe UI Variable Text, Segoe UI");

    private static Brush Freeze(Brush b)
    {
        b.Freeze();
        return b;
    }
}
