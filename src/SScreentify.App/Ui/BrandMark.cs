using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SScreentify.App.Ui;

/// <summary>
/// The SScreentify mark from Diego's Claude Design canvas «SScreentify — Logo»: crop marks framing an S, on a 100×100 box.
/// Small sizes use the canvas's heavier 32/24/16 px variants so the mark stays legible.
/// </summary>
public static class BrandMark
{
    private const string Corners = "M6 26V6H26M74 6H94V26M94 74V94H74M26 94H6V74";
    private const string SmallCorners = "M6 30V6H30M70 6H94V30M94 70V94H70M30 94H6V70";
    private const string Letter = "M66 34H42A8 8 0 0 0 42 50H58A8 8 0 0 1 58 66H34";

    private static readonly Color Ink = Color.FromRgb(0x11, 0x11, 0x11);

    /// <summary>The bare mark sized for <paramref name="pixels"/> device pixels, in a 100×100 box.</summary>
    public static Drawing Mark(Color color, double pixels)
    {
        var (corners, cornerWidth, letterWidth) = pixels switch
        {
            <= 16 => (SmallCorners, 10.0, 14.0),
            <= 24 => (Corners, 8.0, 11.0),
            <= 40 => (Corners, 7.0, 10.0),
            _ => (Corners, 6.0, 9.0),
        };
        var brush = new SolidColorBrush(color);
        var mark = new DrawingGroup();
        mark.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 100, 100))));
        mark.Children.Add(new GeometryDrawing(null,
            new Pen(brush, cornerWidth) { StartLineCap = PenLineCap.Square, EndLineCap = PenLineCap.Square }, Geometry.Parse(corners)));
        mark.Children.Add(new GeometryDrawing(null, new Pen(brush, letterWidth) { LineJoin = PenLineJoin.Round }, Geometry.Parse(Letter)));
        mark.Freeze();
        return mark;
    }

    /// <summary>The app icon: the white mark on a dark rounded tile, in a 100×100 box.</summary>
    public static Drawing AppIcon(double pixels)
    {
        // The canvas puts a 140 px mark on a 220 px tile with 50 px corners; tiny icons enlarge the mark a little.
        var scale = pixels <= 24 ? 0.74 : 140.0 / 220;
        var offset = (100 - 100 * scale) / 2;
        var mark = new DrawingGroup { Transform = new MatrixTransform(scale, 0, 0, scale, offset, offset) };
        mark.Children.Add(Mark(Colors.White, pixels * scale));

        var icon = new DrawingGroup();
        icon.Children.Add(new GeometryDrawing(new SolidColorBrush(Ink), null, new RectangleGeometry(new Rect(0, 0, 100, 100), 50 / 2.2, 50 / 2.2)));
        icon.Children.Add(mark);
        icon.Freeze();
        return icon;
    }

    /// <summary>The notification-area mark: white on a dark taskbar, near-black on a light one.</summary>
    public static Drawing Tray(double pixels, bool lightTaskbar) => Mark(lightTaskbar ? Ink : Colors.White, pixels);

    public static byte[] AppIconPng(int size) => Png(AppIcon(size), size);

    public static byte[] TrayPng(int size, bool lightTaskbar) => Png(Tray(size, lightTaskbar), size);

    public static System.Drawing.Icon TrayIcon(int size, bool lightTaskbar)
    {
        using var stream = new MemoryStream(Ico(new[] { (size, TrayPng(size, lightTaskbar)) }));
        return new System.Drawing.Icon(stream, size, size);
    }

    /// <summary>A multi-size .ico whose entries are PNG images.</summary>
    public static byte[] Ico(IReadOnlyList<(int Size, byte[] Png)> images)
    {
        using var stream = new MemoryStream();
        using var output = new BinaryWriter(stream);
        output.Write((short)0);
        output.Write((short)1);
        output.Write((short)images.Count);
        var offset = 6 + 16 * images.Count;
        foreach (var (size, png) in images)
        {
            output.Write((byte)(size >= 256 ? 0 : size));
            output.Write((byte)(size >= 256 ? 0 : size));
            output.Write((byte)0);
            output.Write((byte)0);
            output.Write((short)1);
            output.Write((short)32);
            output.Write(png.Length);
            output.Write(offset);
            offset += png.Length;
        }
        foreach (var (_, png) in images) output.Write(png);
        output.Flush();
        return stream.ToArray();
    }

    private static byte[] Png(Drawing drawing, int size)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.PushTransform(new ScaleTransform(size / 100.0, size / 100.0));
            dc.DrawDrawing(drawing);
        }
        var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}
