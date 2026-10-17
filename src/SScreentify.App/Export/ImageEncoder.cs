using System.Windows.Media.Imaging;
using SScreentify.App.Imaging;
using SScreentify.Core.Imaging;
using SScreentify.Core.Settings;

namespace SScreentify.App.Export;

public static class ImageEncoder
{
    /// <summary>Encodes without metadata (frames carry no BitmapMetadata).</summary>
    public static byte[] Encode(PixelBuffer image, ImageFormat format, int jpegQuality)
    {
        BitmapEncoder encoder = format switch
        {
            ImageFormat.Png => new PngBitmapEncoder(),
            ImageFormat.Jpeg => new JpegBitmapEncoder { QualityLevel = Math.Clamp(jpegQuality, 1, 100) },
            ImageFormat.Bmp => new BmpBitmapEncoder(),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
        encoder.Frames.Add(BitmapFrame.Create(image.ToBitmapSource()));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}
