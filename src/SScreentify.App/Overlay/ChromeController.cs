using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SScreentify.App.Ui;
using SScreentify.Core.Geometry;

namespace SScreentify.App.Overlay;

/// <summary>Places the tools and actions bars next to the selection, in the window of the selection's monitor.</summary>
public sealed class ChromeController
{
    private const double GapDip = 8;
    private readonly Popup _colorPopup;
    private OverlayWindow? _host;
    private bool _visible;

    public ChromeController()
    {
        var palette = new ColorPalette();
        _colorPopup = new Popup
        {
            Child = palette,
            StaysOpen = false,
            AllowsTransparency = true,
            Placement = PlacementMode.Left,
            PlacementTarget = Tools.ColorButton,
            HorizontalOffset = -6,
        };
        palette.ColorChosen += c =>
        {
            if (Tools.Session is { } s) s.Color = c;
            _colorPopup.IsOpen = false;
        };
        Tools.ColorRequested += () => _colorPopup.IsOpen = !_colorPopup.IsOpen;
        Tools.UndoRequested += () => Tools.Session?.Document.Undo();
        Tools.RedoRequested += () => Tools.Session?.Document.Redo();
    }

    public ToolsBar Tools { get; } = new();
    public ActionsBar Actions { get; } = new();

    /// <summary>Extra bars that follow the tools bar (the censor style bar).</summary>
    public FrameworkElement? Secondary { get; set; }

    public void Attach(CaptureSession session) => Tools.Session = session;

    public void Detach()
    {
        Tools.Session = null;
        _colorPopup.IsOpen = false;
        Hide();
        MoveTo(null);
    }

    public void Hide()
    {
        _visible = false;
        Tools.Visibility = Actions.Visibility = Visibility.Collapsed;
        if (Secondary is not null) Secondary.Visibility = Visibility.Collapsed;
    }

    public void Update(CaptureSession session, IReadOnlyList<OverlayWindow> windows)
    {
        if (session.Selection.Rect is not { } sel || !session.Selection.IsUsable)
        {
            Hide();
            return;
        }

        var monitor = OverlayLayout.MonitorForSelection(session.Capture.Monitors, sel);
        var host = windows.FirstOrDefault(w => w.Monitor.Bounds == monitor.Bounds) ?? windows.First();
        MoveTo(host);

        Tools.Visibility = Actions.Visibility = Visibility.Visible;
        var scale = host.Scale;
        var gap = (int)Math.Round(GapDip * scale);
        var toolsRect = ToolbarPlacement.PlaceVertical(sel, PhysicalSize(Tools, scale), monitor.Bounds, gap);
        var actionsRect = ToolbarPlacement.PlaceHorizontal(sel, PhysicalSize(Actions, scale), monitor.Bounds, gap);
        Place(Tools, Tools.Panel, toolsRect, session, host);
        Place(Actions, Actions.Panel, actionsRect, session, host);

        if (Secondary is not null && Secondary.Visibility == Visibility.Visible)
        {
            var size = PhysicalSize(Secondary, scale);
            var left = toolsRect.X > sel.X ? toolsRect.Right + gap / 2 : toolsRect.X - gap / 2 - size.Width;
            var secondaryRect = new PixelRect(left, toolsRect.Y, size.Width, size.Height);
            Canvas.SetLeft(Secondary, (secondaryRect.X - monitor.Bounds.X) / scale);
            Canvas.SetTop(Secondary, (secondaryRect.Y - monitor.Bounds.Y) / scale);
        }

        if (!_visible)
        {
            _visible = true;
            FadeIn(Tools);
            FadeIn(Actions);
        }
    }

    private void MoveTo(OverlayWindow? host)
    {
        if (host == _host) return;
        foreach (var element in new FrameworkElement?[] { Tools, Actions, Secondary })
            if (element?.Parent is Panel parent) parent.Children.Remove(element);
        _host = host;
        if (host is null) return;
        host.Chrome.Children.Add(Tools);
        host.Chrome.Children.Add(Actions);
        if (Secondary is not null) host.Chrome.Children.Add(Secondary);
    }

    private static PixelSize PhysicalSize(FrameworkElement element, double scale)
    {
        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var size = element.DesiredSize;
        return new PixelSize((int)Math.Ceiling(size.Width * scale), (int)Math.Ceiling(size.Height * scale));
    }

    private static void Place(FrameworkElement bar, GlassPanel panel, PixelRect rect, CaptureSession session, OverlayWindow host)
    {
        var origin = host.Monitor.Bounds;
        Canvas.SetLeft(bar, (rect.X - origin.X) / host.Scale);
        Canvas.SetTop(bar, (rect.Y - origin.Y) / host.Scale);
        var v = session.Capture.VirtualBounds;
        panel.SetBackdrop(session.Image, new Rect(rect.X - v.X, rect.Y - v.Y, rect.Width, rect.Height));
    }

    private static void FadeIn(FrameworkElement bar)
    {
        var duration = TimeSpan.FromMilliseconds(150);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var shift = new TranslateTransform(0, 6);
        bar.RenderTransform = shift;
        bar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, duration) { EasingFunction = ease });
        shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(6, 0, duration) { EasingFunction = ease });
    }
}
