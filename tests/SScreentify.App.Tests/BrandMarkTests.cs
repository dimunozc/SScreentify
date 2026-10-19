using System.Windows.Media;
using System.Windows.Media.Imaging;
using SScreentify.App.Ui;

namespace SScreentify.App.Tests;

public class BrandMarkTests
{
    private static BitmapSource Decode(byte[] png) => BitmapFrame.Create(new MemoryStream(png));

    private static (byte B, byte G, byte R, byte A) Pixel(BitmapSource image, int x, int y)
    {
        var px = new byte[4];
        new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0).CopyPixels(new System.Windows.Int32Rect(x, y, 1, 1), px, 4, 0);
        return (px[0], px[1], px[2], px[3]);
    }

    [Fact]
    public void AppIcon_IsDarkRoundedSquareWithWhiteMark()
    {
        StaRunner.Run(() =>
        {
            var image = Decode(BrandMark.AppIconPng(256));
            Assert.Equal(256, image.PixelWidth);
            Assert.Equal(0, Pixel(image, 1, 1).A);                       // rounded corner stays transparent
            Assert.Equal(((byte)0x11, (byte)0x11, (byte)0x11, (byte)255), Pixel(image, 128, 50)); // dark tile
            var stroke = Pixel(image, 128, 128);                          // middle bar of the S
            Assert.True(stroke.R > 240 && stroke.A == 255, $"expected white, got {stroke}");
        });
    }

    [Theory]
    [InlineData(false, 255)]
    [InlineData(true, 0x11)]
    public void TrayMark_FollowsTaskbarTheme(bool lightTaskbar, int ink)
    {
        StaRunner.Run(() =>
        {
            var image = Decode(BrandMark.TrayPng(32, lightTaskbar));
            Assert.Equal(0, Pixel(image, 16, 2).A);                          // no tile behind the mark
            var corner = Pixel(image, 2, 2);                                  // top-left crop mark
            Assert.Equal(255, corner.A);
            Assert.Equal(ink, corner.R);
        });
    }

    [Theory]
    [InlineData(16)]
    [InlineData(20)]
    [InlineData(32)]
    public void Icons_LoadAtRequestedSize(int size)
    {
        StaRunner.Run(() =>
        {
            using var tray = BrandMark.TrayIcon(size, lightTaskbar: false);
            Assert.Equal(size, tray.Width);
        });
    }
}
