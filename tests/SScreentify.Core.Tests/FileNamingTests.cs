using SScreentify.Core.Files;
using SScreentify.Core.Geometry;
using SScreentify.Core.Settings;

namespace SScreentify.Core.Tests;

public class FileNamingTests
{
    [Theory]
    [InlineData(ImageFormat.Png, "Captura_2026-10-17_16-52-03.png")]
    [InlineData(ImageFormat.Jpeg, "Captura_2026-10-17_16-52-03.jpg")]
    [InlineData(ImageFormat.Bmp, "Captura_2026-10-17_16-52-03.bmp")]
    public void DefaultName_Formats(ImageFormat format, string expected)
    {
        Assert.Equal(expected, FileNaming.DefaultName(new DateTime(2026, 10, 17, 16, 52, 3), format));
    }

    [Fact]
    public void AvailablePath_KeepsNameWhenFree()
    {
        Assert.Equal(Path.Combine("f", "Captura.png"), FileNaming.AvailablePath("f", "Captura.png", _ => false));
    }

    [Fact]
    public void AvailablePath_NumbersTakenNames()
    {
        var taken = new HashSet<string> { Path.Combine("f", "Captura.png"), Path.Combine("f", "Captura (2).png") };
        Assert.Equal(Path.Combine("f", "Captura (3).png"), FileNaming.AvailablePath("f", "Captura.png", taken.Contains));
    }

    [Fact]
    public void MonitorInfo_IsValueEqual()
    {
        var a = new MonitorInfo(new PixelRect(0, 0, 10, 10), new PixelRect(0, 0, 10, 9), 1.5, true);
        Assert.Equal(a, a with { });
    }
}
