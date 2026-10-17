using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace SScreentify.App.Ui;

/// <summary>Base for the Options and About windows: Cristal oscuro colors and a dark title bar.</summary>
public abstract class DarkWindow : Window
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    protected DarkWindow()
    {
        Background = new SolidColorBrush(Color.FromRgb(0x1C, 0x20, 0x2C));
        Foreground = Palette.Text;
        FontFamily = Palette.Font;
        FontSize = 13;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ShowInTaskbar = true;
        UseLayoutRounding = true;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var dark = 1;
        DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
