using System.Globalization;
using SScreentify.Core.Settings;

namespace SScreentify.Core.Files;

public static class FileNaming
{
    public static string DefaultName(DateTime local, ImageFormat f, Language language) =>
        (language == Language.English ? "Screenshot_" : "Captura_")
        + local.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + Extension(f);

    /// <summary>The path for <paramref name="name"/> in <paramref name="folder"/>, numbered like Explorer
    /// ("Captura (2).png") when that name is taken, so a new capture never replaces an earlier one.</summary>
    public static string AvailablePath(string folder, string name, Func<string, bool> exists)
    {
        var path = Path.Combine(folder, name);
        var stem = Path.GetFileNameWithoutExtension(name);
        var extension = Path.GetExtension(name);
        for (var n = 2; exists(path); n++)
            path = Path.Combine(folder, $"{stem} ({n}){extension}");
        return path;
    }

    public static string Extension(ImageFormat f) => f switch
    {
        ImageFormat.Png => ".png",
        ImageFormat.Jpeg => ".jpg",
        ImageFormat.Bmp => ".bmp",
        _ => throw new ArgumentOutOfRangeException(nameof(f)),
    };
}
