using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SScreentify.App.Capture;
using SScreentify.App.Overlay;
using SScreentify.App.Rendering;
using SScreentify.Core.Annotations;
using SScreentify.App.Ui;
using SScreentify.Core.Geometry;
using SScreentify.Core.Imaging;
using SScreentify.Core.Settings;

namespace SScreentify.App.Tests;

/// <summary>
/// Renders the selector chrome over a synthetic desktop to a PNG for visual review.
/// Writes only when SSCREENTIFY_SNAPSHOT_DIR is set; otherwise it just checks the composition renders.
/// </summary>
public class VisualSnapshotTests
{
    internal static ResourceDictionary LoadTheme()
    {
        // pack:// URIs resolve only once a WPF Application exists in the process.
        lock (typeof(VisualSnapshotTests))
            if (Application.Current is null) _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        return new ResourceDictionary { Source = new Uri("pack://application:,,,/sScreentify;component/Ui/Theme.xaml") };
    }

    internal static PixelBuffer SyntheticDesktop(int w, int h)
    {
        var bytes = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var i = (y * w + x) * 4;
            var window = x > 60 && x < 460 && y > 50 && y < 330;
            bytes[i] = window ? (byte)244 : (byte)(200 - y / 4);
            bytes[i + 1] = window ? (byte)245 : (byte)(90 + x / 10);
            bytes[i + 2] = window ? (byte)247 : (byte)(60 + x / 5);
            bytes[i + 3] = 255;
        }
        return new PixelBuffer(w, h, bytes);
    }

    [Fact]
    public void Chrome_RendersOverSyntheticDesktop()
    {
        StaRunner.Run(() =>
        {
            const int w = 800, h = 500;
            var bounds = new PixelRect(0, 0, w, h);
            var capture = new CaptureResult(SyntheticDesktop(w, h), bounds, new[] { new MonitorInfo(bounds, bounds, 1, true) });
            var session = new CaptureSession(capture, new AppSettings());
            var selection = new PixelRect(120, 70, 300, 190);
            session.Selection.Set(selection);
            session.Tool = ToolKind.Arrow;
            var red = Rgba.Parse("#F43F5E");
            var label = AnnotationRenderer.MeasureText("¡aquí!", 20);
            session.Document.Add(new ShapeAnnotation(Guid.NewGuid(), red, 4, new RectD(150, 118, 120, 46), IsEllipse: false));
            session.Document.Add(new SegmentAnnotation(Guid.NewGuid(), red, 4, new PointD(355, 120), new PointD(290, 152), HasArrowHead: true));
            session.Document.Add(new TextAnnotation(Guid.NewGuid(), red, new PointD(330, 90), "¡aquí!", 20, new RectD(330, 90, label.Width, label.Height)));
            session.Document.Add(new PenAnnotation(Guid.NewGuid(), Rgba.Parse("#FACC15"), 4, new[] { new PointD(142, 202), new PointD(292, 202) }, IsMarker: true));
            session.Document.Add(new CensorAnnotation(Guid.NewGuid(), red, new RectD(300, 190, 100, 50), CensorStyle.Pixelate));
            session.Document.Add(new CensorAnnotation(Guid.NewGuid(), red, new RectD(150, 220, 80, 30), CensorStyle.Blur));

            var root = new Grid { Width = w, Height = h };
            root.Resources.MergedDictionaries.Add(LoadTheme());
            root.Children.Add(new Image { Source = session.Image, Stretch = Stretch.None });
            root.Children.Add(new AnnotationLayer { Session = session, MonitorBounds = bounds, Patches = new CensorPatchCache(capture.Pixels, bounds) });
            root.Children.Add(new SelectionAdorner { Session = session, MonitorBounds = bounds });
            var chrome = new Canvas();
            root.Children.Add(chrome);

            var tools = new ToolsBar { Session = session };
            var actions = new ActionsBar();
            chrome.Children.Add(tools);
            chrome.Children.Add(actions);
            foreach (var (bar, panel, vertical) in new (FrameworkElement, GlassPanel, bool)[] { (tools, tools.Panel, true), (actions, actions.Panel, false) })
            {
                bar.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var size = new PixelSize((int)Math.Ceiling(bar.DesiredSize.Width), (int)Math.Ceiling(bar.DesiredSize.Height));
                var rect = vertical ? ToolbarPlacement.PlaceVertical(selection, size, bounds, 8) : ToolbarPlacement.PlaceHorizontal(selection, size, bounds, 8);
                Canvas.SetLeft(bar, rect.X);
                Canvas.SetTop(bar, rect.Y);
                panel.SetBackdrop(session.Image, new Rect(rect.X, rect.Y, rect.Width, rect.Height));
            }

            root.Measure(new Size(w, h));
            root.Arrange(new Rect(0, 0, w, h));
            root.UpdateLayout();
            var bitmap = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(root);

            Assert.True(tools.ActualHeight > 300);

            var dir = Environment.GetEnvironmentVariable("SSCREENTIFY_SNAPSHOT_DIR");
            if (string.IsNullOrEmpty(dir)) return;
            Directory.CreateDirectory(dir);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var file = File.Create(Path.Combine(dir, "chrome.png"));
            encoder.Save(file);
        });
    }

    [Fact]
    public void Windows_RenderForReview()
    {
        StaRunner.Run(() =>
        {
            var theme = LoadTheme();
            Save(Snapshot(new OptionsWindow(new AppSettings { Format = ImageFormat.Jpeg }), theme, 560), "options.png");
            Save(Snapshot(new ToastWindow(string.Format(Strings.ToastSaved, @"C:\Users\Diego\Pictures\sScreentify\Captura_2026-10-17_16-52-03.png"), () => { }, false), theme, 400), "toast.png");
            Save(Snapshot(new ToastWindow(string.Format(Strings.ToastHotkeyFailed, "Ctrl + Mayús + Impr Pant"), () => { }, true), theme, 400), "toast-error.png");
            Save(Snapshot(new AboutWindow(), theme, 380), "about.png");
        });
    }

    private static RenderTargetBitmap Snapshot(Window window, ResourceDictionary theme, double width)
    {
        var content = (FrameworkElement)window.Content;
        window.Content = null;
        var root = new Border { Background = window.Background ?? Brushes.Transparent, Child = content, Width = width };
        TextElement.SetForeground(root, window.Foreground);
        TextElement.SetFontFamily(root, window.FontFamily);
        TextElement.SetFontSize(root, window.FontSize);
        root.Resources.MergedDictionaries.Add(theme);
        root.Measure(new Size(width, double.PositiveInfinity));
        root.Arrange(new Rect(root.DesiredSize));
        root.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth), (int)Math.Ceiling(root.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(root);
        return bitmap;
    }

    private static void Save(BitmapSource bitmap, string name)
    {
        Assert.True(bitmap.PixelHeight > 20);
        var dir = Environment.GetEnvironmentVariable("SSCREENTIFY_SNAPSHOT_DIR");
        if (string.IsNullOrEmpty(dir)) return;
        Directory.CreateDirectory(dir);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(dir, name));
        encoder.Save(file);
    }
}
