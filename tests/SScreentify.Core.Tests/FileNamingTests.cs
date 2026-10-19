using SScreentify.Core.Files;
using SScreentify.Core.Geometry;
using SScreentify.Core.Settings;

namespace SScreentify.Core.Tests;

public class FileNamingTests
{
    [Theory]
    [InlineData(ImageFormat.Png, Language.Spanish, "Captura_2026-10-17_16-52-03.png")]
    [InlineData(ImageFormat.Jpeg, Language.Spanish, "Captura_2026-10-17_16-52-03.jpg")]
    [InlineData(ImageFormat.Bmp, Language.Spanish, "Captura_2026-10-17_16-52-03.bmp")]
    [InlineData(ImageFormat.Png, Language.English, "Screenshot_2026-10-17_16-52-03.png")]
    public void DefaultName_Formats(ImageFormat format, Language language, string expected)
    {
        Assert.Equal(expected, FileNaming.DefaultName(new DateTime(2026, 10, 17, 16, 52, 3), format, language));
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
