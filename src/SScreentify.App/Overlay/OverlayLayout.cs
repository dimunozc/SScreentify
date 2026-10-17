using SScreentify.Core.Geometry;

namespace SScreentify.App.Overlay;

public static class OverlayLayout
{
    /// <summary>One overlay window per monitor, covering exactly that monitor.</summary>
    public static IReadOnlyList<PixelRect> WindowRects(IReadOnlyList<MonitorInfo> monitors) =>
        monitors.Select(m => m.Bounds).ToArray();

    /// <summary>Same monitors with the same bounds, work areas, scales and primary flag (a scale-only change counts as different).</summary>
    public static bool SameLayout(IReadOnlyList<MonitorInfo> a, IReadOnlyList<MonitorInfo> b) => a.SequenceEqual(b);

    /// <summary>The monitor that hosts the toolbars: contains the selection center, else overlaps it most, else the primary.</summary>
    public static MonitorInfo MonitorForSelection(IReadOnlyList<MonitorInfo> monitors, PixelRect sel)
    {
        var center = sel.Center;
        var containing = monitors.FirstOrDefault(m => m.Bounds.Contains(center));
        if (containing is not null) return containing;

        var best = monitors
            .Select(m => (Monitor: m, Area: (long)m.Bounds.Intersect(sel).Width * m.Bounds.Intersect(sel).Height))
            .OrderByDescending(x => x.Area)
            .First();
        return best.Area > 0 ? best.Monitor : monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0];
    }
}
