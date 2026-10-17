using System.Windows;
using Microsoft.Win32;
using SScreentify.App.Services;
using SScreentify.Core.Files;
using SScreentify.Core.Settings;

namespace SScreentify.App.Export;

public static class SaveService
{
    private static readonly ImageFormat[] FilterOrder = { ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.Bmp };

    /// <summary>The last folder if it still exists; otherwise the default folder (created if needed);
    /// if that cannot be created (unplugged drive, no permission), the fallback (PicturessScreentify).</summary>
    public static string ResolveInitialFolder(string? lastFolder, string? defaultFolder, string? fallback = null)
    {
        if (!string.IsNullOrEmpty(lastFolder) && Directory.Exists(lastFolder)) return lastFolder;
        fallback ??= AppPaths.DefaultPicturesFolder;
        var folder = string.IsNullOrEmpty(defaultFolder) ? fallback : defaultFolder;
        try
        {
            Directory.CreateDirectory(folder);
            return folder;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Directory.CreateDirectory(fallback);
            return fallback;
        }
    }

    public static string? AskPath(Window? owner, AppSettings s)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG (*.png)|*.png|JPG (*.jpg)|*.jpg|BMP (*.bmp)|*.bmp",
            FilterIndex = Array.IndexOf(FilterOrder, s.Format) + 1,
            FileName = FileNaming.DefaultName(DateTime.Now, s.Format),
            InitialDirectory = ResolveInitialFolder(s.LastSaveFolder, s.DefaultFolder),
            AddExtension = true,
            OverwritePrompt = true,
        };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }

    public static ImageFormat FormatFromPath(string path, ImageFormat fallback) =>
        Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => ImageFormat.Png,
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".bmp" => ImageFormat.Bmp,
            _ => fallback,
        };

    /// <summary>Writes through a temporary file so a failed write never leaves a truncated image.</summary>
    public static void Write(string path, byte[] data)
    {
        var temp = path + ".tmp";
        File.WriteAllBytes(temp, data);
        File.Move(temp, path, overwrite: true);
    }
}
