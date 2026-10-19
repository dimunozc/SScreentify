using System.Runtime.CompilerServices;
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

    /// <summary>
    /// Gaussian-looking blur (sigma = radius / 2) built from three box blurs per axis with running sums, so the cost per
    /// pixel does not grow with the radius and the patch can follow the mouse while dragging. Samples outside src are
    /// clamped to its edge.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)] // runs on every mouse move; skip the slow first JIT tier
    public static PixelBuffer Blur(PixelBuffer src, PixelRect region, int radius = DefaultBlurRadius)
    {
        var area = ClampRegion(src, region);
        var boxes = BoxRadii(Math.Max(0.5, radius / 2.0));

        // Work on the region plus every pixel that can reach it through the three passes.
        var margin = boxes.Sum();
        int w = area.Width + 2 * margin, h = area.Height + 2 * margin;
        var plane = new float[w * h];
        var scratch = new float[w * h];
        var line = new float[Math.Max(w, h)];
        var output = new byte[area.Width * area.Height * 4];

        for (var c = 0; c < 3; c++)
        {
            for (var y = 0; y < h; y++)
            {
                var row = Math.Clamp(area.Y - margin + y, 0, src.Height - 1) * src.Width;
                for (var x = 0; x < w; x++)
                    plane[y * w + x] = src.Bgra[(row + Math.Clamp(area.X - margin + x, 0, src.Width - 1)) * 4 + c];
            }

            foreach (var r in boxes)
                for (var y = 0; y < h; y++)
                    BoxRow(plane, y * w, w, r, line);
            foreach (var r in boxes)
            {
                BoxColumns(plane, scratch, w, h, r);
                (plane, scratch) = (scratch, plane);
            }

            for (var y = 0; y < area.Height; y++)
            for (var x = 0; x < area.Width; x++)
                output[(y * area.Width + x) * 4 + c] = ToByte(plane[(y + margin) * w + x + margin]);
        }
        for (var o = 3; o < output.Length; o += 4) output[o] = 255;

        return new PixelBuffer(area.Width, area.Height, output);
    }

    /// <summary>Radii of three box blurs whose combination approximates a gaussian of the given sigma.</summary>
    private static int[] BoxRadii(double sigma)
    {
        const int passes = 3;
        var lower = (int)Math.Floor(Math.Sqrt(12 * sigma * sigma / passes + 1));
        if (lower % 2 == 0) lower--;
        var upper = lower + 2;
        var lowerCount = (int)Math.Round((12 * sigma * sigma - passes * lower * lower - 4 * passes * lower - 3 * passes) / (-4.0 * lower - 4));
        return Enumerable.Range(0, passes).Select(i => (i < lowerCount ? lower : upper) / 2).ToArray();
    }

    /// <summary>Box blur of one row in place; samples past the ends repeat the edge.</summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void BoxRow(float[] data, int start, int n, int r, float[] line)
    {
        Array.Copy(data, start, line, 0, n);
        float sum = 0;
        for (var k = -r; k <= r; k++) sum += line[Math.Clamp(k, 0, n - 1)];
        var scale = 1f / (2 * r + 1);
        for (var i = 0; i < n; i++)
        {
            data[start + i] = sum * scale;
            sum += line[Math.Min(i + r + 1, n - 1)] - line[Math.Max(i - r, 0)];
        }
    }

    /// <summary>Box blur of every column from <paramref name="src"/> into <paramref name="dst"/>, one row at a time so memory
    /// is read in order; samples past the ends repeat the edge.</summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void BoxColumns(float[] src, float[] dst, int w, int h, int r)
    {
        var sums = new float[w];
        for (var k = -r; k <= r; k++)
        {
            var row = Math.Clamp(k, 0, h - 1) * w;
            for (var x = 0; x < w; x++) sums[x] += src[row + x];
        }
        var scale = 1f / (2 * r + 1);
        for (var y = 0; y < h; y++)
        {
            int add = Math.Min(y + r + 1, h - 1) * w, remove = Math.Max(y - r, 0) * w, row = y * w;
            for (var x = 0; x < w; x++)
            {
                dst[row + x] = sums[x] * scale;
                sums[x] += src[add + x] - src[remove + x];
            }
        }
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

    private static byte ToByte(float v) => (byte)Math.Clamp((int)Math.Round(v), 0, 255);
}
