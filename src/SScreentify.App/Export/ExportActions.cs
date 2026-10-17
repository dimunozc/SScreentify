using System.Diagnostics;
using SScreentify.App.Capture;
using SScreentify.App.Overlay;
using SScreentify.App.Services;
using SScreentify.App.Ui;
using SScreentify.Core.Files;
using SScreentify.Core.Imaging;
using SScreentify.Core.Settings;

namespace SScreentify.App.Export;

/// <summary>Copy, save, print and instant full-screen save. Failures keep the selector open so nothing is lost.</summary>
public sealed class ExportActions
{
    private readonly OverlayController _overlay;
    private readonly Func<AppSettings> _settings;
    private readonly Action<AppSettings> _save;
    private readonly ToastService _toasts;

    public ExportActions(OverlayController overlay, Func<AppSettings> settings, Action<AppSettings> save, ToastService toasts)
    {
        _overlay = overlay;
        _settings = settings;
        _save = save;
        _toasts = toasts;
        overlay.CopyRequested += Copy;
        overlay.SaveRequested += Save;
        overlay.PrintRequested += Print;
    }

    public void Copy(CaptureSession s)
    {
        if (Render(s) is not { } image) return;
        if (ClipboardService.Copy(image) == ClipboardResult.Busy)
        {
            _toasts.Show(Strings.ToastCopyBusy, isError: true);
            return;
        }
        _overlay.Close();
        if (_settings().ShowNotifications) _toasts.Show(Strings.ToastCopied);
    }

    public void Save(CaptureSession s)
    {
        if (Render(s) is not { } image) return;
        var settings = _settings();
        var path = SaveService.AskPath(_overlay.HostWindow, settings);
        if (path is null) return;

        try
        {
            SaveService.Write(path, ImageEncoder.Encode(image, SaveService.FormatFromPath(path, settings.Format), settings.JpegQuality));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _toasts.Show(string.Format(Strings.ToastSaveFailed, ex.Message), isError: true);
            return;
        }

        _save(_settings() with { LastSaveFolder = Path.GetDirectoryName(path) });
        _overlay.Close();
        NotifySaved(path);
    }

    public void Print(CaptureSession s)
    {
        if (Render(s) is not { } image) return;
        if (PrintService.Print(image)) _overlay.Close();
    }

    public void SaveFullscreenInstant()
    {
        var settings = _settings();
        var capture = ScreenCapture.CaptureVirtualScreen(settings.CaptureCursor);
        try
        {
            var folder = SaveService.ResolveInitialFolder(null, settings.DefaultFolder);
            var path = Path.Combine(folder, FileNaming.DefaultName(DateTime.Now, settings.Format));
            SaveService.Write(path, ImageEncoder.Encode(capture.Pixels, settings.Format, settings.JpegQuality));
            NotifySaved(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _toasts.Show(string.Format(Strings.ToastSaveFailed, ex.Message), isError: true);
        }
    }

    private PixelBuffer? Render(CaptureSession s)
    {
        _overlay.FinishEditing();
        if (!s.Selection.IsUsable || s.Selection.Rect is not { } selection) return null;
        return Flattener.Flatten(s.Capture.Pixels, s.Capture.VirtualBounds, selection, s.Document.Items);
    }

    private void NotifySaved(string path)
    {
        if (!_settings().ShowNotifications) return;
        _toasts.Show(string.Format(Strings.ToastSaved, path),
            onClick: () => Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true }));
    }
}
