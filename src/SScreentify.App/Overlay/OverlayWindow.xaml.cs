using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using SScreentify.App.Native;
using SScreentify.Core.Geometry;

namespace SScreentify.App.Overlay;

/// <summary>Covers one monitor; draws the frozen capture in physical coordinates and forwards input to the controller.</summary>
public partial class OverlayWindow : Window
{
    private readonly OverlayController _controller;

    public OverlayWindow(OverlayController controller, MonitorInfo monitor)
    {
        _controller = controller;
        Monitor = monitor;
        InitializeComponent();
        Adorner.MonitorBounds = monitor.Bounds;
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

    public void Attach(CaptureSession session)
    {
        Session = session;
        Adorner.Session = session;
        CaptureImage.Source = session.Image;
        UpdateImageTransform();
        Adorner.InvalidateVisual();
    }

    public void Detach()
    {
        Session = null;
        Adorner.Session = null;
        CaptureImage.Source = null;
        AnnotationHost.Children.Clear();
    }

    public void ShowOverlay()
    {
        Show();
        Place();
    }

    public void Redraw() => Adorner.InvalidateVisual();

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
