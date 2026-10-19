using SScreentify.App.Ui;

namespace SScreentify.App.Diagnostics;

/// <summary>Writes the app icon as a multi-size .ico (PNG entries). Produces Assets/app.ico (--export-icon PATH).</summary>
public static class IconExport
{
    private static readonly int[] Sizes = { 16, 20, 24, 32, 48, 64, 256 };

    public static void Write(string path) =>
        File.WriteAllBytes(path, BrandMark.Ico(Sizes.Select(size => (size, BrandMark.AppIconPng(size))).ToList()));
}
