using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using SScreentify.App.Export;
using SScreentify.App.Imaging;
using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;
using SScreentify.Core.Imaging;
using SScreentify.Core.Settings;

namespace SScreentify.App.Tests;

public class ExportTests
{
    private static readonly PixelRect Origin = new(0, 0, 100, 100);

    private static PixelBuffer Gradient(int w = 100, int h = 100)
    {
        var bytes = new byte[w * h * 4];
        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var i = (y * w + x) * 4;
            (bytes[i], bytes[i + 1], bytes[i + 2], bytes[i + 3]) = ((byte)x, (byte)y, (byte)(x ^ y), 255);
        }
        return new PixelBuffer(w, h, bytes);
    }

    private static PixelBuffer RandomImage(int w = 100, int h = 100)
    {
        var bytes = new byte[w * h * 4];
        new Random(7).NextBytes(bytes);
        for (var i = 3; i < bytes.Length; i += 4) bytes[i] = 255;
        return new PixelBuffer(w, h, bytes);
    }

    private static void AssertBlack(PixelBuffer image, PixelRect area)
    {
        for (var y = area.Y; y < area.Bottom; y++)
        for (var x = area.X; x < area.Right; x++)
            Assert.Equal(new Rgba(0, 0, 0, 255), image.GetPixel(x, y));
    }

    [Fact]
    public void Flatten_CropsToSelection()
    {
        StaRunner.Run(() =>
        {
            var capture = Gradient();
            var result = Flattener.Flatten(capture, Origin, new PixelRect(10, 20, 30, 40), Array.Empty<Annotation>());
            Assert.Equal(30, result.Width);
            Assert.Equal(40, result.Height);
            Assert.Equal(capture.GetPixel(10, 20), result.GetPixel(0, 0));
            Assert.Equal(capture.GetPixel(39, 59), result.GetPixel(29, 39));
        });
    }

    [Fact]
    public void Flatten_HonorsNegativeVirtualOrigin()
    {
        StaRunner.Run(() =>
        {
            var capture = Gradient();
            var origin = new PixelRect(-50, -10, 100, 100);
            var result = Flattener.Flatten(capture, origin, new PixelRect(-40, 0, 10, 10), Array.Empty<Annotation>());
            Assert.Equal(capture.GetPixel(10, 10), result.GetPixel(0, 0));
        });
    }

    [Fact]
    public void Flatten_SolidCensorLeavesNoOriginalPixels()
    {
        StaRunner.Run(() =>
        {
            var censor = new CensorAnnotation(Guid.NewGuid(), Rgba.Parse("#000000"), new RectD(15, 25, 10, 10), CensorStyle.Solid);
            var result = Flattener.Flatten(RandomImage(), Origin, new PixelRect(0, 0, 60, 60), new Annotation[] { censor });
            AssertBlack(result, new PixelRect(15, 25, 10, 10));

            var decoded = new PngBitmapDecoder(new MemoryStream(ImageEncoder.Encode(result, ImageFormat.Png, 90)),
                BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0].ToPixelBuffer();
            AssertBlack(decoded, new PixelRect(15, 25, 10, 10));
        });
    }

    [Fact]
    public void Flatten_SolidCensorWithFractionalEdgesCoversWholePixels()
    {
        StaRunner.Run(() =>
        {
            var censor = new CensorAnnotation(Guid.NewGuid(), Rgba.Parse("#000000"), new RectD(15.4, 25.6, 9.3, 9.1), CensorStyle.Solid);
            var result = Flattener.Flatten(RandomImage(), Origin, new PixelRect(0, 0, 60, 60), new Annotation[] { censor });
            AssertBlack(result, new PixelRect(15, 25, 10, 10));
        });
    }

    [Fact]
    public void Flatten_CensorCoversEarlierAnnotation()
    {
        StaRunner.Run(() =>
        {
            var red = new ShapeAnnotation(Guid.NewGuid(), Rgba.Parse("#FF0000"), 6, new RectD(10, 10, 30, 30), IsEllipse: false);
            var censor = new CensorAnnotation(Guid.NewGuid(), Rgba.Parse("#000000"), new RectD(5, 5, 40, 40), CensorStyle.Solid);
            var result = Flattener.Flatten(Gradient(), Origin, new PixelRect(0, 0, 60, 60), new Annotation[] { red, censor });
            AssertBlack(result, new PixelRect(5, 5, 40, 40));
        });
    }

    [Fact]
    public void Flatten_PixelateComesFromOriginalCapture()
    {
        StaRunner.Run(() =>
        {
            var capture = RandomImage();
            var censor = new CensorAnnotation(Guid.NewGuid(), Rgba.Parse("#000000"), new RectD(24, 24, 24, 24), CensorStyle.Pixelate);
            var result = Flattener.Flatten(capture, Origin, new PixelRect(0, 0, 60, 60), new Annotation[] { censor });
            var expected = Censor.Pixelate(capture, new PixelRect(24, 24, 24, 24));
            Assert.Equal(expected.GetPixel(0, 0), result.GetPixel(24, 24));
            Assert.Equal(expected.GetPixel(23, 23), result.GetPixel(47, 47));
        });
    }

    [Fact]
    public void Flatten_ClipsAnnotationsOutsideSelection()
    {
        StaRunner.Run(() =>
        {
            var line = new SegmentAnnotation(Guid.NewGuid(), Rgba.Parse("#FF0000"), 4, new PointD(-20, 5), new PointD(200, 5), HasArrowHead: true);
            var result = Flattener.Flatten(Gradient(), Origin, new PixelRect(10, 0, 20, 20), new Annotation[] { line });
            Assert.Equal(20, result.Width);
            Assert.Equal(20, result.Height);
            Assert.Equal(255, result.GetPixel(10, 5).R);
        });
    }

    [Fact]
    public void Flatten_RejectsUnusableSelection()
    {
        StaRunner.Run(() =>
            Assert.Throws<ArgumentException>(() => Flattener.Flatten(Gradient(), Origin, new PixelRect(5, 5, 1, 1), Array.Empty<Annotation>())));
    }

    [Fact]
    public void Encode_PngHasNoTextChunks()
    {
        StaRunner.Run(() =>
        {
            var latin = Encoding.Latin1.GetString(ImageEncoder.Encode(Gradient(), ImageFormat.Png, 90));
            Assert.StartsWith("\u0089PNG", latin);
            Assert.DoesNotContain("tEXt", latin);
            Assert.DoesNotContain("iTXt", latin);
            Assert.DoesNotContain("zTXt", latin);
        });
    }

    [Fact]
    public void Encode_JpegQualityChangesSize()
    {
        StaRunner.Run(() =>
        {
            var image = RandomImage();
            Assert.True(ImageEncoder.Encode(image, ImageFormat.Jpeg, 30).Length < ImageEncoder.Encode(image, ImageFormat.Jpeg, 95).Length);
            Assert.Equal((byte)'B', ImageEncoder.Encode(image, ImageFormat.Bmp, 90)[0]);
        });
    }

    [Fact]
    public void Copy_OffersPngAndBitmap()
    {
        StaRunner.Run(() =>
        {
            IDataObject? offered = null;
            Assert.Equal(ClipboardResult.Ok, ClipboardService.Copy(Gradient(10, 10), d => offered = d));
            Assert.True(offered!.GetDataPresent("PNG"));
            Assert.True(offered.GetDataPresent(DataFormats.Bitmap));
        });
    }

    [Fact]
    public void ResolveInitialFolder_FallsBackWhenMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), "sScreentify.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var fallback = Path.Combine(root, "Capturas");
            Assert.Equal(fallback, SaveService.ResolveInitialFolder(Path.Combine(root, "no-existe"), fallback));
            Assert.True(Directory.Exists(fallback));
            Assert.Equal(root, SaveService.ResolveInitialFolder(root, fallback));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveInitialFolder_SkipsFoldersWithoutWritePermission()
    {
        var root = Path.Combine(Path.GetTempPath(), "sScreentify.Tests", Guid.NewGuid().ToString("N"));
        var locked = new DirectoryInfo(Path.Combine(root, "solo-lectura"));
        var deny = new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.CreateFiles, AccessControlType.Deny);
        try
        {
            locked.Create();
            var acl = locked.GetAccessControl();
            acl.AddAccessRule(deny);
            locked.SetAccessControl(acl);
            var fallback = Path.Combine(root, "Capturas");

            Assert.Equal(fallback, SaveService.ResolveInitialFolder(locked.FullName, null, fallback)); // last folder
            Assert.Equal(fallback, SaveService.ResolveInitialFolder(null, locked.FullName, fallback)); // default folder
            Assert.Empty(locked.GetFileSystemInfos());
        }
        finally
        {
            if (locked.Exists)
            {
                var acl = locked.GetAccessControl();
                acl.RemoveAccessRule(deny);
                locked.SetAccessControl(acl);
            }
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Write_WhenReplaceFails_LeavesNoTempFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "sScreentify.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            // A folder where the image should go makes the final move fail after the temporary file is written.
            var target = Path.Combine(root, "Captura.png");
            Directory.CreateDirectory(target);
            Assert.ThrowsAny<Exception>(() => SaveService.Write(target, new byte[] { 1, 2, 3 }));
            Assert.Equal(new[] { target }, Directory.GetFileSystemEntries(root));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData(@"C:\a\b.png", ImageFormat.Png)]
    [InlineData(@"C:\a\b.JPG", ImageFormat.Jpeg)]
    [InlineData(@"C:\a\b.jpeg", ImageFormat.Jpeg)]
    [InlineData(@"C:\a\b.bmp", ImageFormat.Bmp)]
    [InlineData(@"C:\a\b", ImageFormat.Png)]
    public void FormatFromPath_UsesExtension(string path, ImageFormat expected)
    {
        Assert.Equal(expected, SaveService.FormatFromPath(path, ImageFormat.Png));
    }
}
