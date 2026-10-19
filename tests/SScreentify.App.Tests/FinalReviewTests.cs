using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Input;
using SScreentify.App.Capture;
using SScreentify.App.Export;
using SScreentify.App.Overlay;
using SScreentify.App.Services;
using SScreentify.Core.Geometry;
using SScreentify.Core.Imaging;
using SScreentify.Core.Settings;

namespace SScreentify.App.Tests;

/// <summary>Regressions for the whole-branch review findings.</summary>
public class FinalReviewTests
{
    private sealed class RecordingToasts : IToastSink
    {
        public List<(string Message, bool IsError)> Shown { get; } = new();

        public void Show(string message, Action? onClick = null, bool isError = false) =>
            Shown.Add((message, isError));
    }

    private static CaptureResult Synthetic(int w = 64, int h = 64)
    {
        var bytes = new byte[w * h * 4];
        for (var i = 3; i < bytes.Length; i += 4) bytes[i] = 255;
        // No monitors: the controller has no windows, so nothing is shown on screen.
        return new CaptureResult(new PixelBuffer(w, h, bytes), new PixelRect(0, 0, w, h), Array.Empty<MonitorInfo>());
    }

    private static OverlayController Headless(Action<AppSettings>? save = null, Func<bool, CaptureResult>? capture = null) =>
        new(() => new AppSettings(), save ?? (_ => { }), capture ?? (_ => Synthetic()), () => Array.Empty<MonitorInfo>());

    [Fact]
    public void Close_WhenSettingsSaveFails_StillReleasesGate()
    {
        StaRunner.Run(() =>
        {
            var overlay = Headless(save: _ => throw new IOException("settings.json is locked"));
            overlay.Open();
            Assert.NotNull(overlay.Session);

            overlay.Close();
            Assert.Null(overlay.Session);

            overlay.Open();
            Assert.NotNull(overlay.Session);
        });
    }

    [Fact]
    public void Open_WhenCaptureFails_ReportsAndReleasesGate()
    {
        StaRunner.Run(() =>
        {
            var calls = 0;
            var overlay = Headless(capture: _ => ++calls == 1 ? throw new Win32Exception("BitBlt falló.") : Synthetic());
            Exception? reported = null;
            overlay.Failed += ex => reported = ex;

            overlay.Open();
            Assert.IsType<Win32Exception>(reported);
            Assert.Null(overlay.Session);

            overlay.Open();
            Assert.NotNull(overlay.Session);
        });
    }

    [Fact]
    public void DisplayChange_WithUnchangedMonitors_KeepsSession()
    {
        StaRunner.Run(() =>
        {
            var overlay = Headless();
            overlay.Open();
            overlay.OnDisplaySettingsChanged();
            Assert.NotNull(overlay.Session);
        });
    }

    [Fact]
    public void SameLayout_DetectsScaleOnlyChange()
    {
        var bounds = new PixelRect(0, 0, 1920, 1080);
        var at100 = new[] { new MonitorInfo(bounds, bounds, 1.0, true) };
        var at150 = new[] { new MonitorInfo(bounds, bounds, 1.5, true) };
        Assert.True(OverlayLayout.SameLayout(at100, new[] { at100[0] with { } }));
        Assert.False(OverlayLayout.SameLayout(at100, at150));
    }

    [Fact]
    public void SaveFullscreenInstant_IgnoredWhileSelectorOpen()
    {
        StaRunner.Run(() =>
        {
            var overlay = Headless();
            var captures = 0;
            var export = new ExportActions(overlay, () => new AppSettings(), _ => { }, new RecordingToasts(), _ => { captures++; return Synthetic(); });
            overlay.Open();
            export.SaveFullscreenInstant();
            Assert.Equal(0, captures);
        });
    }

    [Fact]
    public void SaveFullscreenInstant_TwiceInARowKeepsBothFiles()
    {
        var folder = Path.Combine(Path.GetTempPath(), "SScreentify.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            StaRunner.Run(() =>
            {
                var toasts = new RecordingToasts();
                var export = new ExportActions(Headless(), () => new AppSettings { DefaultFolder = folder }, _ => { }, toasts, _ => Synthetic());
                export.SaveFullscreenInstant();
                export.SaveFullscreenInstant();
                Assert.DoesNotContain(toasts.Shown, t => t.IsError);
            });
            Assert.Equal(2, Directory.GetFiles(folder, "*.png").Length);
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void SaveFullscreenInstant_CaptureFailureShowsError()
    {
        StaRunner.Run(() =>
        {
            var toasts = new RecordingToasts();
            var export = new ExportActions(Headless(), () => new AppSettings(), _ => { }, toasts, _ => throw new Win32Exception("BitBlt falló."));
            export.SaveFullscreenInstant();
            Assert.Single(toasts.Shown, t => t.IsError);
        });
    }

    [Fact]
    public void Copy_WhenLocked_GivesUpQuickly()
    {
        StaRunner.Run(() =>
        {
            var attempts = 0;
            var result = ClipboardService.Copy(Synthetic(4, 4).Pixels, _ =>
            {
                attempts++;
                throw new COMException("CLIPBRD_E_CANT_OPEN", unchecked((int)0x800401D0));
            }, retryDelay: TimeSpan.Zero);
            Assert.Equal(ClipboardResult.Busy, result);
            Assert.Equal(2, attempts);
        });
    }

    [Fact]
    public void Copy_OtherClipboardError_DoesNotThrow()
    {
        StaRunner.Run(() =>
        {
            var result = ClipboardService.Copy(Synthetic(4, 4).Pixels, _ => throw new COMException("OleFlushClipboard", unchecked((int)0x80004005)));
            Assert.Equal(ClipboardResult.Failed, result);
        });
    }

    [Fact]
    public void ResolveInitialFolder_UncreatableDefaultFallsBack()
    {
        var root = Path.Combine(Path.GetTempPath(), "SScreentify.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "not-a-folder");
            File.WriteAllText(file, "x");
            var fallback = Path.Combine(root, "fallback");
            Assert.Equal(fallback, SaveService.ResolveInitialFolder(null, Path.Combine(file, "sub"), fallback));
            Assert.True(Directory.Exists(fallback));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ToolKeys_IgnoredWithoutUsableSelection()
    {
        var session = ToolbarTests.NewSession();
        var tools = new ToolController(session);
        Assert.False(tools.OnKey(Key.V, ModifierKeys.None));
        Assert.Equal(ToolKind.None, session.Tool);
    }

    [Fact]
    public void Tool_ResetsWhenSelectionBecomesUnusable()
    {
        var session = ToolbarTests.NewSession();
        _ = new ToolController(session);
        session.Selection.Set(new PixelRect(0, 0, 20, 20));
        session.Tool = ToolKind.Pen;
        session.Selection.Clear();
        Assert.Equal(ToolKind.None, session.Tool);
    }
}
