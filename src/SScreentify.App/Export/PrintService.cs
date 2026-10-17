using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SScreentify.App.Imaging;
using SScreentify.Core.Imaging;

namespace SScreentify.App.Export;

public static class PrintService
{
    /// <summary>Shows the print dialog; returns false if the user cancelled.</summary>
    public static bool Print(PixelBuffer image)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return false;

        double areaW = dialog.PrintableAreaWidth, areaH = dialog.PrintableAreaHeight;
        var scale = Math.Min(1, Math.Min(areaW / image.Width, areaH / image.Height));
        var page = new Image
        {
            Source = image.ToBitmapSource(),
            Width = image.Width * scale,
            Height = image.Height * scale,
            Stretch = Stretch.Uniform,
        };
        page.Measure(new Size(areaW, areaH));
        page.Arrange(new Rect(0, 0, page.Width, page.Height));
        dialog.PrintVisual(page, "sScreentify");
        return true;
    }
}
