using System.Globalization;
using SScreentify.Core.Settings;

namespace SScreentify.Core.Files;

public static class FileNaming
{
    public static string DefaultName(DateTime local, ImageFormat f) =>
        "Captura_" + local.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + Extension(f);

    public static string Extension(ImageFormat f) => f switch
    {
        ImageFormat.Png => ".png",
        ImageFormat.Jpeg => ".jpg",
        ImageFormat.Bmp => ".bmp",
        _ => throw new ArgumentOutOfRangeException(nameof(f)),
    };
}
