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
    private readonly IToastSink _toasts;
    private readonly Func<bool, CaptureResult> _capture;

    public ExportActions(OverlayController overlay, Func<AppSettings> settings, Action<AppSettings> save, IToastSink toasts,
        Func<bool, CaptureResult>? capture = null)
    {
        _overlay = overlay;
        _settings = settings;
        _save = save;
        _toasts = toasts;
        _capture = capture ?? ScreenCapture.CaptureVirtualScreen;
        overlay.CopyRequested += Copy;
        overlay.SaveRequested += Save;
        overlay.PrintRequested += Print;
    }

    public void Copy(CaptureSession s)
    {
        if (Render(s) is not { } image) return;
        switch (ClipboardService.Copy(image))
        {
            case ClipboardResult.Busy:
                _toasts.Show(Strings.ToastCopyBusy, isError: true);
                return;
            case ClipboardResult.Failed:
                _toasts.Show(Strings.ToastCopyFailed, isError: true);
                return;
        }
        _overlay.Close();
        if (_settings().ShowNotifications) _toasts.Show(Strings.ToastCopied);
    }

    public void Save(CaptureSession s)
    {
        if (Render(s) is not { } image) return;
        var settings = _settings();
        string? path;
        try
        {
            path = SaveService.AskPath(_overlay.HostWindow, settings);
            if (path is null) return;
            SaveService.Write(path, ImageEncoder.Encode(image, SaveService.FormatFromPath(path, settings.Format), settings.JpegQuality));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            _toasts.Show(string.Format(Strings.ToastSaveFailed, ex.Message), isError: true);
            return;
        }

        try
        {
            _save(_settings() with { LastSaveFolder = Path.GetDirectoryName(path) });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Debug.WriteLine($"settings not saved: {ex.Message}");
        }
        _overlay.Close();
        NotifySaved(path);
    }

    public void Print(CaptureSession s)
    {
        if (Render(s) is not { } image) return;
        try
        {
            // PrintDialog owns itself to MainWindow; make that the visible overlay so the dialog is not hidden behind it.
            if (System.Windows.Application.Current is { } app) app.MainWindow = _overlay.HostWindow;
            if (PrintService.Print(image)) _overlay.Close();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Offline printers and cancelled print-to-file prompts throw; keep the capture.
            _toasts.Show(string.Format(Strings.ToastPrintFailed, ex.Message), isError: true);
        }
    }

    /// <summary>Saves the whole desktop without the selector. Ignored while the selector is open, which would photograph it.</summary>
    public void SaveFullscreenInstant()
    {
        if (_overlay.Session is not null) return;
        var settings = _settings();
        try
        {
            var capture = _capture(settings.CaptureCursor);
            var folder = SaveService.ResolveInitialFolder(null, settings.DefaultFolder);
            var path = FileNaming.AvailablePath(folder, FileNaming.DefaultName(DateTime.Now, settings.Format, UiLanguage.Current), File.Exists);
            SaveService.Write(path, ImageEncoder.Encode(capture.Pixels, settings.Format, settings.JpegQuality));
            NotifySaved(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
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
