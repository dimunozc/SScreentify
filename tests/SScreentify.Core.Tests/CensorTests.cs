using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;
using SScreentify.Core.Imaging;

namespace SScreentify.Core.Tests;

public class CensorTests
{
    private static PixelBuffer Random(int w, int h, int seed = 42)
    {
        var bytes = new byte[w * h * 4];
        new Random(seed).NextBytes(bytes);
        for (var i = 3; i < bytes.Length; i += 4) bytes[i] = 255;
        return new PixelBuffer(w, h, bytes);
    }

    private static PixelBuffer Fill(int w, int h, Func<int, int, Rgba> color)
    {
        var bytes = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var c = color(x, y);
            var i = (y * w + x) * 4;
            (bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3]) = (c.B, c.G, c.R, c.A);
        }
        return new PixelBuffer(w, h, bytes);
    }

    [Fact]
    public void Constructor_RejectsWrongLength()
    {
        Assert.Throws<ArgumentException>(() => new PixelBuffer(2, 2, new byte[15]));
    }

    [Fact]
    public void Solid_ContainsNoOriginalPixel()
    {
        var patch = Censor.Solid(new PixelRect(8, 8, 32, 32), Rgba.Parse("#000000"));
        Assert.Equal(32, patch.Width);
        Assert.Equal(32, patch.Height);
        for (var y = 0; y < 32; y++)
        for (var x = 0; x < 32; x++)
            Assert.Equal(new Rgba(0, 0, 0, 255), patch.GetPixel(x, y));
    }

    [Fact]
    public void Pixelate_BlocksAreUniformAverage()
    {
        var src = Fill(24, 24, (x, y) => new Rgba((byte)(x * 10), (byte)(y * 10), 7));
        var patch = Censor.Pixelate(src, new PixelRect(0, 0, 24, 24), block: 12);

        foreach (var (bx, by) in new[] { (0, 0), (12, 0), (0, 12), (12, 12) })
        {
            double r = 0, g = 0;
            for (var y = by; y < by + 12; y++)
            for (var x = bx; x < bx + 12; x++) { r += x * 10; g += y * 10; }
            var expected = new Rgba((byte)Math.Round(r / 144), (byte)Math.Round(g / 144), 7);
            for (var y = by; y < by + 12; y++)
            for (var x = bx; x < bx + 12; x++)
                Assert.Equal(expected, patch.GetPixel(x, y));
        }
    }

    [Fact]
    public void Pixelate_PartialEdgeBlock()
    {
        var src = Fill(30, 12, (x, _) => x < 24 ? new Rgba(0, 0, 0) : new Rgba(200, 100, 50));
        var patch = Censor.Pixelate(src, new PixelRect(0, 0, 30, 12), block: 12);
        Assert.Equal(30, patch.Width);
        Assert.Equal(new Rgba(200, 100, 50), patch.GetPixel(29, 11));
        Assert.Equal(new Rgba(200, 100, 50), patch.GetPixel(24, 0));
        Assert.Equal(new Rgba(0, 0, 0), patch.GetPixel(23, 0));
    }

    [Fact]
    public void Blur_ChangesSharpEdge()
    {
        var src = Fill(64, 8, (x, _) => x < 32 ? new Rgba(0, 0, 0) : new Rgba(255, 255, 255));
        var patch = Censor.Blur(src, new PixelRect(0, 0, 64, 8), radius: 16);
        var edge = patch.GetPixel(32, 4).R;
        Assert.InRange(edge, (byte)64, (byte)191);
        Assert.Equal(255, patch.GetPixel(32, 4).A);
    }

    [Fact]
    public void Blur_KeepsUniformColor()
    {
        var src = Fill(80, 60, (_, _) => new Rgba(30, 140, 220));
        var patch = Censor.Blur(src, new PixelRect(10, 10, 50, 30));
        for (var y = 0; y < patch.Height; y++)
        for (var x = 0; x < patch.Width; x++)
            Assert.Equal(new Rgba(30, 140, 220), patch.GetPixel(x, y));
    }

    [Theory]
    [InlineData(20, 20, 100, 70)]
    [InlineData(0, 0, 60, 40)]
    [InlineData(120, 90, 40, 30)]
    public void Blur_LooksLikeGaussian(int x, int y, int w, int h)
    {
        // Blocks with hard edges plus a gradient: the worst case for approximating a gaussian.
        var src = Fill(160, 120, (px, py) => ((px / 24) + (py / 24)) % 2 == 0
            ? new Rgba((byte)(px * 255 / 159), 20, 200)
            : new Rgba(240, (byte)(py * 2), 10));
        var region = new PixelRect(x, y, w, h);
        var patch = Censor.Blur(src, region);
        var reference = ReferenceGaussian(src, region, Censor.DefaultBlurRadius);

        var worst = 0;
        for (var py = 0; py < h; py++)
        for (var px = 0; px < w; px++)
        {
            var (a, b) = (patch.GetPixel(px, py), reference(px, py));
            worst = Math.Max(worst, Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B))));
        }
        Assert.InRange(worst, 0, 12);
    }

    /// <summary>Direct separable gaussian, sigma = radius / 2, samples clamped to src: the look Blur must keep.</summary>
    private static Func<int, int, Rgba> ReferenceGaussian(PixelBuffer src, PixelRect region, int radius)
    {
        var sigma = radius / 2.0;
        var kernel = Enumerable.Range(-radius, 2 * radius + 1).Select(i => Math.Exp(-(i * i) / (2 * sigma * sigma))).ToArray();
        var total = kernel.Sum();
        double Channel(int cx, int cy, Func<Rgba, byte> pick)
        {
            double sum = 0;
            for (var ky = -radius; ky <= radius; ky++)
            for (var kx = -radius; kx <= radius; kx++)
            {
                var p = src.GetPixel(Math.Clamp(cx + kx, 0, src.Width - 1), Math.Clamp(cy + ky, 0, src.Height - 1));
                sum += pick(p) * kernel[kx + radius] * kernel[ky + radius];
            }
            return sum / (total * total);
        }
        return (px, py) =>
        {
            int cx = region.X + px, cy = region.Y + py;
            return new Rgba((byte)Math.Round(Channel(cx, cy, c => c.R)), (byte)Math.Round(Channel(cx, cy, c => c.G)), (byte)Math.Round(Channel(cx, cy, c => c.B)));
        };
    }

    [Fact]
    public void Crop_ClampsAndRejectsEmpty()
    {
        var src = Random(10, 10);
        var crop = src.Crop(new PixelRect(5, 5, 20, 20));
        Assert.Equal(5, crop.Width);
        Assert.Equal(src.GetPixel(5, 5), crop.GetPixel(0, 0));
        Assert.Equal(src.GetPixel(9, 9), crop.GetPixel(4, 4));
        Assert.Throws<ArgumentException>(() => src.Crop(new PixelRect(20, 20, 5, 5)));
    }

    [Fact]
    public void Region_OutsideBounds_IsClamped()
    {
        var src = Random(64, 64);
        Assert.Equal(10, Censor.Pixelate(src, new PixelRect(-10, -10, 20, 20)).Width);
        Assert.Equal(10, Censor.Blur(src, new PixelRect(-10, -10, 20, 20)).Height);
    }
}
