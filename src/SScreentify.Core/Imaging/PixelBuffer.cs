using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;

namespace SScreentify.Core.Imaging;

/// <summary>A BGRA32 (straight alpha) image; stride is Width * 4.</summary>
public sealed class PixelBuffer
{
    public PixelBuffer(int width, int height, byte[] bgra)
    {
        if (width <= 0 || height <= 0 || bgra.Length != width * height * 4)
            throw new ArgumentException($"Se esperaban {width}×{height}×4 bytes y hay {bgra.Length}.", nameof(bgra));
        Width = width;
        Height = height;
        Bgra = bgra;
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] Bgra { get; }
    public PixelRect Bounds => new(0, 0, Width, Height);

    public Rgba GetPixel(int x, int y)
    {
        var i = (y * Width + x) * 4;
        return new Rgba(Bgra[i + 2], Bgra[i + 1], Bgra[i], Bgra[i + 3]);
    }

    public PixelBuffer Crop(PixelRect r)
    {
        var area = Bounds.Intersect(r);
        if (area.IsEmpty)
            throw new ArgumentException("La región no corta la imagen.", nameof(r));

        var bytes = new byte[area.Width * area.Height * 4];
        var rowBytes = area.Width * 4;
        for (var y = 0; y < area.Height; y++)
            Buffer.BlockCopy(Bgra, ((area.Y + y) * Width + area.X) * 4, bytes, y * rowBytes, rowBytes);
        return new PixelBuffer(area.Width, area.Height, bytes);
    }
}
