using System.Windows.Media.Imaging;
using SScreentify.App.Imaging;
using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;
using SScreentify.Core.Imaging;

namespace SScreentify.App.Rendering;

/// <summary>Pixelate/blur patches computed from the ORIGINAL capture, cached per censor geometry.</summary>
public sealed class CensorPatchCache
{
    private readonly PixelBuffer _capture;
    private readonly PixelRect _captureOrigin;
    private readonly Dictionary<(Guid, PixelRect, CensorStyle), (PixelRect Area, BitmapSource Patch)> _patches = new();

    /// <param name="captureOrigin">Where the capture's top-left sits in capture coordinates (the virtual desktop origin).</param>
    public CensorPatchCache(PixelBuffer capture, PixelRect captureOrigin)
    {
        _capture = capture;
        _captureOrigin = captureOrigin;
    }

    /// <summary>The patch and the capture-coordinate area it covers, or null when the censor misses the capture.</summary>
    public (PixelRect Area, BitmapSource Patch)? Get(CensorAnnotation a)
    {
        var region = CoverPixels(a.Rect);
        var key = (a.Id, region, a.Style);
        if (_patches.TryGetValue(key, out var cached)) return cached;

        var local = new PixelRect(region.X - _captureOrigin.X, region.Y - _captureOrigin.Y, region.Width, region.Height);
        var area = _capture.Bounds.Intersect(local);
        if (area.IsEmpty) return null;

        var patch = a.Style == CensorStyle.Blur ? Censor.Blur(_capture, area) : Censor.Pixelate(_capture, area);
        var result = (new PixelRect(area.X + _captureOrigin.X, area.Y + _captureOrigin.Y, area.Width, area.Height), patch.ToBitmapSource());
        foreach (var stale in _patches.Keys.Where(k => k.Item1 == a.Id).ToList()) _patches.Remove(stale);
        _patches[key] = result;
        return result;
    }

    /// <summary>The smallest whole-pixel rectangle covering <paramref name="r"/>.</summary>
    public static PixelRect CoverPixels(RectD r)
    {
        int left = (int)Math.Floor(r.X), top = (int)Math.Floor(r.Y);
        int right = (int)Math.Ceiling(r.Right), bottom = (int)Math.Ceiling(r.Bottom);
        return new PixelRect(left, top, right - left, bottom - top);
    }
}
