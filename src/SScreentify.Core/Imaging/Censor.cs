using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;

namespace SScreentify.Core.Imaging;

/// <summary>
/// Censor patches. Each returns an opaque patch the size of region ∩ src bounds,
/// computed from the original capture pixels.
/// </summary>
public static class Censor
{
    public const int DefaultBlock = 12;
    public const int DefaultBlurRadius = 16;

    public static PixelBuffer Pixelate(PixelBuffer src, PixelRect region, int block = DefaultBlock)
    {
        var area = ClampRegion(src, region);
        var output = new byte[area.Width * area.Height * 4];

        for (var by = 0; by < area.Height; by += block)
        for (var bx = 0; bx < area.Width; bx += block)
        {
            int w = Math.Min(block, area.Width - bx), h = Math.Min(block, area.Height - by);
            long b = 0, g = 0, r = 0;
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = ((area.Y + by + y) * src.Width + area.X + bx + x) * 4;
                b += src.Bgra[i];
                g += src.Bgra[i + 1];
                r += src.Bgra[i + 2];
            }

            double n = w * h;
            byte mb = (byte)Math.Round(b / n), mg = (byte)Math.Round(g / n), mr = (byte)Math.Round(r / n);
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var o = ((by + y) * area.Width + bx + x) * 4;
                (output[o], output[o + 1], output[o + 2], output[o + 3]) = (mb, mg, mr, 255);
            }
        }

        return new PixelBuffer(area.Width, area.Height, output);
    }

    /// <summary>Separable gaussian blur, sigma = radius / 2; samples outside the region are clamped to src.</summary>
    public static PixelBuffer Blur(PixelBuffer src, PixelRect region, int radius = DefaultBlurRadius)
    {
        var area = ClampRegion(src, region);
        var kernel = Kernel(radius);

        // Horizontal pass over the region's columns, extended vertically by the radius.
        int top = Math.Max(0, area.Y - radius), bottom = Math.Min(src.Height, area.Bottom + radius);
        var rows = bottom - top;
        var temp = new float[area.Width * rows * 3];
        for (var y = 0; y < rows; y++)
        for (var x = 0; x < area.Width; x++)
        {
            float b = 0, g = 0, r = 0;
            for (var k = -radius; k <= radius; k++)
            {
                var sx = Math.Clamp(area.X + x + k, 0, src.Width - 1);
                var i = ((top + y) * src.Width + sx) * 4;
                var weight = kernel[k + radius];
                b += src.Bgra[i] * weight;
                g += src.Bgra[i + 1] * weight;
                r += src.Bgra[i + 2] * weight;
            }
            var t = (y * area.Width + x) * 3;
            (temp[t], temp[t + 1], temp[t + 2]) = (b, g, r);
        }

        var output = new byte[area.Width * area.Height * 4];
        for (var y = 0; y < area.Height; y++)
        for (var x = 0; x < area.Width; x++)
        {
            float b = 0, g = 0, r = 0;
            for (var k = -radius; k <= radius; k++)
            {
                var sy = Math.Clamp(area.Y + y + k, top, bottom - 1) - top;
                var t = (sy * area.Width + x) * 3;
                var weight = kernel[k + radius];
                b += temp[t] * weight;
                g += temp[t + 1] * weight;
                r += temp[t + 2] * weight;
            }
            var o = (y * area.Width + x) * 4;
            (output[o], output[o + 1], output[o + 2], output[o + 3]) = (ToByte(b), ToByte(g), ToByte(r), 255);
        }

        return new PixelBuffer(area.Width, area.Height, output);
    }

    public static PixelBuffer Solid(PixelRect region, Rgba color)
    {
        if (region.IsEmpty)
            throw new ArgumentException("La región está vacía.", nameof(region));

        var output = new byte[region.Width * region.Height * 4];
        for (var o = 0; o < output.Length; o += 4)
            (output[o], output[o + 1], output[o + 2], output[o + 3]) = (color.B, color.G, color.R, 255);
        return new PixelBuffer(region.Width, region.Height, output);
    }

    private static PixelRect ClampRegion(PixelBuffer src, PixelRect region)
    {
        var area = src.Bounds.Intersect(region);
        return area.IsEmpty ? throw new ArgumentException("La región no corta la imagen.", nameof(region)) : area;
    }

    private static float[] Kernel(int radius)
    {
        var sigma = Math.Max(0.5, radius / 2.0);
        var kernel = new float[2 * radius + 1];
        double sum = 0;
        for (var i = -radius; i <= radius; i++)
        {
            var v = Math.Exp(-(i * i) / (2 * sigma * sigma));
            kernel[i + radius] = (float)v;
            sum += v;
        }
        for (var i = 0; i < kernel.Length; i++) kernel[i] = (float)(kernel[i] / sum);
        return kernel;
    }

    private static byte ToByte(float v) => (byte)Math.Clamp((int)Math.Round(v), 0, 255);
}
