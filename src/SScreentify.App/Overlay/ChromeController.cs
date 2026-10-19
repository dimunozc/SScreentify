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
    private readonly ColorPalette _palette;
    private OverlayWindow? _host;
    private bool _visible;

    public ChromeController()
    {
        var palette = _palette = new ColorPalette();
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

    /// <summary>Follows the tools bar while the Censor tool is active.</summary>
    public CensorBar Censor { get; } = new() { Visibility = Visibility.Collapsed };

    /// <summary>Re-reads every tooltip and label after the interface language changed.</summary>
    public void RefreshTexts()
    {
        foreach (var texts in new[] { Tools.Texts, Actions.Texts, Censor.Texts, _palette.Texts }) texts.Refresh();
    }

    public void Attach(CaptureSession session)
    {
        Tools.Session = session;
        Censor.Session = session;
    }

    public void Detach()
    {
        Tools.Session = null;
        Censor.Session = null;
        // The backdrops reference the capture bitmap; drop them so it can be freed.
        foreach (var panel in new[] { Tools.Panel, Actions.Panel, Censor.Panel }) panel.SetBackdrop(null, Rect.Empty);
        _colorPopup.IsOpen = false;
        Hide();
        MoveTo(null);
    }

    public void Hide()
    {
        _visible = false;
        Tools.Visibility = Actions.Visibility = Visibility.Collapsed;
        Censor.Visibility = Visibility.Collapsed;
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

        Censor.Visibility = session.Tool == ToolKind.Censor ? Visibility.Visible : Visibility.Collapsed;
        if (Censor.Visibility == Visibility.Visible)
        {
            // Beside the tools bar, on the side away from the selection; kept inside the monitor.
            var size = PhysicalSize(Censor, scale);
            var left = toolsRect.X >= sel.Right ? toolsRect.Right + gap / 2 : toolsRect.X - gap / 2 - size.Width;
            if (left + size.Width > monitor.Bounds.Right || left < monitor.Bounds.X) left = toolsRect.X - gap / 2 - size.Width;
            var x = Math.Clamp(left, monitor.Bounds.X, monitor.Bounds.Right - size.Width);
            var censorRect = new PixelRect(x, toolsRect.Y, size.Width, size.Height);
            Place(Censor, Censor.Panel, censorRect, session, host);
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
        foreach (var element in new FrameworkElement[] { Tools, Actions, Censor })
            if (element?.Parent is Panel parent) parent.Children.Remove(element);
        _host = host;
        if (host is null) return;
        host.Chrome.Children.Add(Tools);
        host.Chrome.Children.Add(Actions);
        host.Chrome.Children.Add(Censor);
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
