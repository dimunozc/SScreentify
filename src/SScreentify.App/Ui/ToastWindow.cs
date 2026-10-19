using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace SScreentify.App.Ui;

/// <summary>A non-activating Cristal oscuro notification at the bottom-right of the primary work area.</summary>
public sealed class ToastWindow : Window
{
    private const int GWL_EXSTYLE = -20, WS_EX_NOACTIVATE = 0x08000000, WS_EX_TOOLWINDOW = 0x00000080;
    private readonly DispatcherTimer _timer;

    public ToastWindow(string message, Action? onClick, bool isError)
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowActivated = false;
        Topmost = true;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        Title = "sScreentify";

        var text = new TextBlock
        {
            Text = message, TextWrapping = TextWrapping.Wrap, Foreground = Palette.Text,
            FontFamily = Palette.Font, FontSize = 13, VerticalAlignment = VerticalAlignment.Center,
        };
        var dot = new Border
        {
            Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 6, 10, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Background = isError ? new SolidColorBrush(Color.FromRgb(0xF4, 0x3F, 0x5E)) : Palette.Accent,
        };
        var row = new DockPanel();
        DockPanel.SetDock(dot, Dock.Left);
        row.Children.Add(dot);
        row.Children.Add(text);

        Content = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0xF2, 0x1C, 0x20, 0x2C)),
            BorderBrush = Palette.PanelBorder, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(12),
            Padding = new Thickness(14, 10, 14, 12), Margin = new Thickness(12), MaxWidth = 360, Child = row,
            Effect = new DropShadowEffect { BlurRadius = 24, Opacity = 0.45, ShadowDepth = 4, Direction = 270 },
            Cursor = onClick is null ? Cursors.Arrow : Cursors.Hand,
        };

        MouseLeftButtonUp += (_, _) => { onClick?.Invoke(); Close(); };
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(isError ? 6 : 3) };
        _timer.Tick += (_, _) => Close();
        MouseEnter += (_, _) => _timer.Stop();
        MouseLeave += (_, _) => _timer.Start();
        Loaded += (_, _) =>
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - ActualWidth - 4;
            Top = area.Bottom - ActualHeight - 4;
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
            _timer.Start();
        };
        Closed += (_, _) => _timer.Stop();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
}
