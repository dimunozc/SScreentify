using System.Windows.Controls.Primitives;
using SScreentify.App.Capture;
using SScreentify.App.Overlay;
using SScreentify.App.Ui;
using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;
using SScreentify.Core.Imaging;
using SScreentify.Core.Settings;

namespace SScreentify.App.Tests;

public class ToolbarTests
{
    internal static CaptureSession NewSession(int w = 64, int h = 64)
    {
        var bytes = new byte[w * h * 4];
        for (var i = 3; i < bytes.Length; i += 4) bytes[i] = 255;
        var bounds = new PixelRect(0, 0, w, h);
        var capture = new CaptureResult(new PixelBuffer(w, h, bytes), bounds, new[] { new MonitorInfo(bounds, bounds, 1, true) });
        return new CaptureSession(capture, new AppSettings());
    }

    [Fact]
    public void ToolsBar_ClickSetsToolAndClickAgainClears()
    {
        StaRunner.Run(() =>
        {
            var session = NewSession();
            var bar = new ToolsBar { Session = session };
            var arrow = bar.ButtonFor(ToolKind.Arrow);

            arrow.RaiseEvent(new System.Windows.RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(ToolKind.Arrow, session.Tool);
            Assert.True(arrow.IsChecked);

            arrow.RaiseEvent(new System.Windows.RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(ToolKind.None, session.Tool);
            Assert.False(bar.ButtonFor(ToolKind.Arrow).IsChecked);
        });
    }

    [Fact]
    public void ToolsBar_FollowsSessionTool()
    {
        StaRunner.Run(() =>
        {
            var session = NewSession();
            var bar = new ToolsBar { Session = session };
            session.Tool = ToolKind.Censor;
            Assert.True(bar.ButtonFor(ToolKind.Censor).IsChecked);
            Assert.False(bar.ButtonFor(ToolKind.Pen).IsChecked);
        });
    }

    [Fact]
    public void ActionsBar_RaisesCopy()
    {
        StaRunner.Run(() =>
        {
            var bar = new ActionsBar();
            var copied = 0;
            bar.CopyRequested += () => copied++;
            bar.CopyButton.RaiseEvent(new System.Windows.RoutedEventArgs(ButtonBase.ClickEvent));
            Assert.Equal(1, copied);
        });
    }

    [Fact]
    public void ColorPalette_PresetsMatchPlan()
    {
        Assert.Equal(new[] { "#F43F5E", "#F97316", "#FACC15", "#22C55E", "#3B82F6", "#A855F7", "#FFFFFF", "#111827" }, ColorPalette.Presets);
        Assert.All(ColorPalette.Presets, p => Rgba.Parse(p));
    }

    [Fact]
    public void ChromeDetach_ReleasesCaptureBackdrops()
    {
        StaRunner.Run(() =>
        {
            var session = NewSession();
            var chrome = new ChromeController();
            chrome.Attach(session);
            foreach (var panel in new[] { chrome.Tools.Panel, chrome.Actions.Panel, chrome.Censor.Panel })
                panel.SetBackdrop(session.Image, new System.Windows.Rect(0, 0, 10, 10));

            chrome.Detach();

            Assert.False(chrome.Tools.Panel.HasBackdrop);
            Assert.False(chrome.Actions.Panel.HasBackdrop);
            Assert.False(chrome.Censor.Panel.HasBackdrop);
            Assert.Null(chrome.Tools.Session);
        });
    }
}
