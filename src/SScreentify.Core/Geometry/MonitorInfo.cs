namespace SScreentify.Core.Geometry;

/// <summary>A display in physical pixels; DpiScale is effective DPI / 96.</summary>
public sealed record MonitorInfo(PixelRect Bounds, PixelRect WorkArea, double DpiScale, bool IsPrimary);
