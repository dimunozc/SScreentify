using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using SScreentify.App.Native;
using SScreentify.App.Rendering;
using SScreentify.App.Ui;
using SScreentify.Core.Geometry;

namespace SScreentify.App.Overlay;

/// <summary>Covers one monitor; draws the frozen capture in physical coordinates and forwards input to the controller.</summary>
public partial class OverlayWindow : Window
{
    private readonly OverlayController _controller;
    private readonly DispatcherTimer _indicatorTimer = new() { Interval = TimeSpan.FromMilliseconds(800) };
    private readonly Border _indicator;

    public OverlayWindow(OverlayController controller, MonitorInfo monitor)
    {
        _controller = controller;
        Monitor = monitor;
        InitializeComponent();
        Adorner.MonitorBounds = monitor.Bounds;
        Annotations.MonitorBounds = monitor.Bounds;
        _indicator = new Border
        {
            Background = Palette.Pill, CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 3, 8, 3),
            Visibility = Visibility.Collapsed, IsHitTestVisible = false,
            Child = new TextBlock { Foreground = Palette.Text, FontFamily = Palette.Font, FontSize = 12, FontWeight = FontWeights.SemiBold },
        };
        Chrome.Children.Add(_indicator);
        _indicatorTimer.Tick += (_, _) => { _indicatorTimer.Stop(); _indicator.Visibility = Visibility.Collapsed; };
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(Place);
    }

    public MonitorInfo Monitor { get; }
    public CaptureSession? Session { get; private set; }

    public double Scale => VisualTreeHelper.GetDpi(this).DpiScaleX;

    /// <summary>Creates the HWND hidden and moves it onto its monitor so showing it later is instant.</summary>
    public void Prepare()
    {
        new WindowInteropHelper(this).EnsureHandle();
        Place();
    }

    public void Attach(CaptureSession session, ToolController tools, CensorPatchCache patches)
    {
        Session = session;
        Adorner.Session = session;
        Annotations.Session = session;
        Annotations.Tools = tools;
        Annotations.Patches = patches;
        CaptureImage.Source = session.Image;
        UpdateImageTransform();
        Redraw();
    }

    public void Detach()
    {
        Session = null;
        Adorner.Session = null;
        Annotations.Session = null;
        Annotations.Tools = null;
        Annotations.Patches = null;
        CaptureImage.Source = null;
        EditorHost.Children.Clear();
        _indicator.Visibility = Visibility.Collapsed;
    }

    public void ShowOverlay()
    {
        Show();
        Place();
    }

    public void Redraw()
    {
        Adorner.InvalidateVisual();
        Annotations.InvalidateVisual();
    }

    public void RedrawAnnotations() => Annotations.InvalidateVisual();

    /// <summary>Shows a short-lived value pill (stroke width, font size) next to the given physical point.</summary>
    public void ShowIndicator(string text, PointD near)
    {
        ((TextBlock)_indicator.Child).Text = text;
        Canvas.SetLeft(_indicator, (near.X - Monitor.Bounds.X) / Scale + 16);
        Canvas.SetTop(_indicator, (near.Y - Monitor.Bounds.Y) / Scale + 16);
        _indicator.Visibility = Visibility.Visible;
        _indicatorTimer.Stop();
        _indicatorTimer.Start();
    }

    /// <summary>Physical capture coordinates → this window's DIPs.</summary>
    public Matrix PhysicalToDip
    {
        get
        {
            var s = Scale;
            return new Matrix(1 / s, 0, 0, 1 / s, -Monitor.Bounds.X / s, -Monitor.Bounds.Y / s);
        }
    }

    public PointD ToPhysical(Point dip)
    {
        var s = Scale;
        return new PointD(Monitor.Bounds.X + dip.X * s, Monitor.Bounds.Y + dip.Y * s);
    }

    private void Place()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;
        var b = Monitor.Bounds;
        User32.SetWindowPos(hwnd, IntPtr.Zero, b.X, b.Y, b.Width, b.Height, User32.SWP_NOZORDER | User32.SWP_NOACTIVATE);
        UpdateImageTransform();
    }

    private void UpdateImageTransform()
    {
        if (Session is null) return;
        var m = PhysicalToDip;
        var origin = Session.Capture.VirtualBounds;
        // The image's own origin is the virtual desktop's top-left corner.
        m.TranslatePrepend(origin.X, origin.Y);
        CaptureImage.RenderTransform = new MatrixTransform(m);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Handled) return;
        CaptureMouse();
        _controller.OnMouseDown(this, ToPhysical(e.GetPosition(this)), e.ChangedButton, Keyboard.Modifiers);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _controller.OnMouseMove(this, ToPhysical(e.GetPosition(this)), Keyboard.Modifiers);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (IsMouseCaptured) ReleaseMouseCapture();
        _controller.OnMouseUp(this, ToPhysical(e.GetPosition(this)), e.ChangedButton, Keyboard.Modifiers);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _controller.OnMouseWheel(this, e.Delta);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (_controller.OnKeyDown(this, key, Keyboard.Modifiers))
            e.Handled = true;
    }
}
