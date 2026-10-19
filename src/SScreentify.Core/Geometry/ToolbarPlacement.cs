namespace SScreentify.Core.Geometry;

/// <summary>Places toolbars next to the selection, inside the selection's monitor.</summary>
public static class ToolbarPlacement
{
    /// <summary>
    /// Whether the size label (<paramref name="heightDip"/> tall, gap included) fits above the selection on some monitor;
    /// otherwise it goes inside. Decided once in virtual-desktop pixels so every monitor's window agrees and the label is
    /// drawn once, even when the selection starts just below the boundary between two monitors.
    /// </summary>
    public static bool SizeLabelFitsAbove(PixelRect selection, double heightDip, IReadOnlyList<MonitorInfo> monitors)
    {
        var origin = new PixelPoint(selection.X, selection.Y);
        var scale = monitors.FirstOrDefault(m => m.Bounds.Contains(origin))?.DpiScale ?? 1.0;
        var top = origin with { Y = selection.Y - (int)Math.Ceiling(heightDip * scale) };
        return monitors.Any(m => m.Bounds.Contains(top));
    }

    public static PixelRect PlaceVertical(PixelRect selection, PixelSize bar, PixelRect monitor, int gap)
    {
        int x, y = selection.Y;
        if (selection.Right + gap + bar.Width <= monitor.Right)
            x = selection.Right + gap;
        else if (selection.X - gap - bar.Width >= monitor.X)
            x = selection.X - gap - bar.Width;
        else
        {
            x = selection.Right - gap - bar.Width;
            y = selection.Y + gap;
        }
        return ClampInto(new PixelRect(x, y, bar.Width, bar.Height), monitor);
    }

    public static PixelRect PlaceHorizontal(PixelRect selection, PixelSize bar, PixelRect monitor, int gap)
    {
        int x = selection.Right - bar.Width, y;
        if (selection.Bottom + gap + bar.Height <= monitor.Bottom)
            y = selection.Bottom + gap;
        else if (selection.Y - gap - bar.Height >= monitor.Y)
            y = selection.Y - gap - bar.Height;
        else
        {
            x = selection.Right - gap - bar.Width;
            y = selection.Bottom - gap - bar.Height;
        }
        return ClampInto(new PixelRect(x, y, bar.Width, bar.Height), monitor);
    }

    private static PixelRect ClampInto(PixelRect r, PixelRect monitor) => r with
    {
        X = Math.Max(monitor.X, Math.Min(r.X, monitor.Right - r.Width)),
        Y = Math.Max(monitor.Y, Math.Min(r.Y, monitor.Bottom - r.Height)),
    };
}
