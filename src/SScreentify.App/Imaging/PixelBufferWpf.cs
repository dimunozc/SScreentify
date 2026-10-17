using System.Windows.Media;
using System.Windows.Media.Imaging;
using SScreentify.Core.Imaging;

namespace SScreentify.App.Imaging;

public static class PixelBufferWpf
{
    /// <summary>96 DPI so one image pixel maps to one physical pixel when the caller scales by the window DPI.</summary>
    public static BitmapSource ToBitmapSource(this PixelBuffer b)
    {
        var source = BitmapSource.Create(b.Width, b.Height, 96, 96, PixelFormats.Bgra32, null, b.Bgra, b.Width * 4);
        source.Freeze();
        return source;
    }

    public static PixelBuffer ToPixelBuffer(this BitmapSource s)
    {
        var converted = s.Format == PixelFormats.Bgra32 ? s : new FormatConvertedBitmap(s, PixelFormats.Bgra32, null, 0);
        var bytes = new byte[converted.PixelWidth * converted.PixelHeight * 4];
        converted.CopyPixels(bytes, converted.PixelWidth * 4, 0);
        return new PixelBuffer(converted.PixelWidth, converted.PixelHeight, bytes);
    }
}
