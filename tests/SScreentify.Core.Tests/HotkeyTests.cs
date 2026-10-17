using SScreentify.Core.Hotkeys;

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
        Assert.Equal("Ctrl + Mayús + Impr Pant", HotkeyRules.Display(On(HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x2C)));
        Assert.Equal("Impr Pant", HotkeyRules.Display(On(HotkeyModifiers.None, 0x2C)));
        Assert.Equal("Alt + A", HotkeyRules.Display(On(HotkeyModifiers.Alt, 0x41)));
        Assert.Equal("Ctrl + Alt + Mayús + Win + F12", HotkeyRules.Display(On(HotkeyModifiers.Win | HotkeyModifiers.Shift | HotkeyModifiers.Alt | HotkeyModifiers.Control, 0x7B)));
        Assert.Equal("Ctrl + 5", HotkeyRules.Display(On(HotkeyModifiers.Control, 0x35)));
        Assert.Equal("Tecla 0xBA", HotkeyRules.Display(On(HotkeyModifiers.None, 0xBA)));
    }
}
