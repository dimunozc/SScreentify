using System.Drawing;
using System.Drawing.Imaging;
using SScreentify.App.Services;

namespace SScreentify.App.Diagnostics;

/// <summary>Writes the app mark as a multi-size .ico (PNG entries). Used once to produce Assets/app.ico (--export-icon PATH).</summary>
public static class IconExport
{
    private static readonly int[] Sizes = { 16, 20, 24, 32, 48, 64, 256 };

    public static void Write(string path)
    {
        var images = Sizes.Select(size =>
        {
            using var icon = TrayIcon.CreateIcon(size);
            using var bitmap = icon.ToBitmap();
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return (Size: size, Png: stream.ToArray());
        }).ToList();

        using var output = new BinaryWriter(File.Create(path));
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
    }
}
