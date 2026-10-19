using SScreentify.Core.Hotkeys;
using SScreentify.Core.Settings;

namespace SScreentify.Core.Tests;

public class HotkeyTests
{
    private static HotkeySetting On(HotkeyModifiers m, int vk) => new(true, m, vk);

    [Fact]
    public void Validate_DuplicateEnabled()
    {
        Assert.Equal(HotkeyValidationError.Duplicate,
            HotkeyRules.Validate(new[] { On(HotkeyModifiers.None, 0x2C), On(HotkeyModifiers.None, 0x2C) }));
    }

    [Fact]
    public void Validate_DuplicateButOneDisabled_IsFine()
    {
        Assert.Equal(HotkeyValidationError.None,
            HotkeyRules.Validate(new[] { On(HotkeyModifiers.None, 0x2C), new HotkeySetting(false, HotkeyModifiers.None, 0x2C) }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0x10)]
    [InlineData(0x11)]
    [InlineData(0x12)]
    [InlineData(0x5B)]
    [InlineData(0xA3)]
    public void Validate_ModifierOnlyOrEmpty_IsInvalid(int vk)
    {
        Assert.Equal(HotkeyValidationError.Invalid, HotkeyRules.Validate(new[] { On(HotkeyModifiers.Control, vk) }));
    }

    [Fact]
    public void Validate_DisabledEmpty_IsFine()
    {
        Assert.Equal(HotkeyValidationError.None, HotkeyRules.Validate(new[] { new HotkeySetting(false, HotkeyModifiers.None, 0) }));
    }

    [Fact]
    public void Display_FormatsInSpanish()
    {
        static string Es(HotkeySetting h) => HotkeyRules.Display(h, Language.Spanish);
        Assert.Equal("Ctrl + Mayús + Impr Pant", Es(On(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x2C)));
        Assert.Equal("Impr Pant", Es(On(HotkeyModifiers.None, 0x2C)));
        Assert.Equal("Alt + A", Es(On(HotkeyModifiers.Alt, 0x41)));
        Assert.Equal("Ctrl + Alt + Mayús + Win + F12", Es(On(HotkeyModifiers.Win | HotkeyModifiers.Shift | HotkeyModifiers.Alt | HotkeyModifiers.Control, 0x7B)));
        Assert.Equal("Ctrl + 5", Es(On(HotkeyModifiers.Control, 0x35)));
        Assert.Equal("Tecla 0xBA", Es(On(HotkeyModifiers.None, 0xBA)));
    }

    [Fact]
    public void Display_FormatsInEnglish()
    {
        static string En(HotkeySetting h) => HotkeyRules.Display(h, Language.English);
        Assert.Equal("Ctrl + Shift + Print Screen", En(On(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x2C)));
        Assert.Equal("Page Up", En(On(HotkeyModifiers.None, 0x21)));
        Assert.Equal("Alt + Delete", En(On(HotkeyModifiers.Alt, 0x2E)));
        Assert.Equal("Key 0xBA", En(On(HotkeyModifiers.None, 0xBA)));
    }

    [Theory]
    [InlineData(0x2C)] [InlineData(0x08)] [InlineData(0x09)] [InlineData(0x0D)] [InlineData(0x13)] [InlineData(0x20)]
    [InlineData(0x21)] [InlineData(0x22)] [InlineData(0x23)] [InlineData(0x24)] [InlineData(0x25)] [InlineData(0x26)]
    [InlineData(0x27)] [InlineData(0x28)] [InlineData(0x2D)] [InlineData(0x2E)]
    public void NamedKeys_AreNamedInBothLanguages(int vk)
    {
        Assert.DoesNotContain("0x", HotkeyRules.Display(On(HotkeyModifiers.None, vk), Language.Spanish));
        Assert.DoesNotContain("0x", HotkeyRules.Display(On(HotkeyModifiers.None, vk), Language.English));
    }

    [Theory]
    [InlineData(LanguagePreference.Auto, "es", Language.Spanish)]
    [InlineData(LanguagePreference.Auto, "en", Language.English)]
    [InlineData(LanguagePreference.Auto, "pt", Language.English)]
    [InlineData(LanguagePreference.Spanish, "en", Language.Spanish)]
    [InlineData(LanguagePreference.English, "es", Language.English)]
    public void Resolve_AutoFollowsWindows(LanguagePreference preference, string windows, Language expected)
    {
        Assert.Equal(expected, Languages.Resolve(preference, windows));
    }
}
