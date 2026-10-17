using SScreentify.App.Services;
using SScreentify.App.Ui;
using SScreentify.Core.Hotkeys;

namespace SScreentify.App.Tests;

public class ShellTests
{
    [Fact]
    public void StaRunner_PropagatesExceptions()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => StaRunner.Run(() => throw new InvalidOperationException("boom")));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public void StaRunner_RunsOnSta()
    {
        ApartmentState? state = null;
        StaRunner.Run(() => state = Thread.CurrentThread.GetApartmentState());
        Assert.Equal(ApartmentState.STA, state);
    }

    [Fact]
    public void HotkeyService_RegisterTwiceSameCombo_SecondFails()
    {
        StaRunner.Run(() =>
        {
            var combo = new HotkeySetting(true, HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift, 0x87); // F24
            using var first = new HotkeyService();
            using var second = new HotkeyService();
            Assert.True(first.Register(HotkeyId.Main, combo));
            Assert.False(second.Register(HotkeyId.Main, combo));
            first.Unregister(HotkeyId.Main);
            Assert.True(second.Register(HotkeyId.Main, combo));
        });
    }

    [Fact]
    public void HotkeyService_DisabledSetting_IsNotRegistered()
    {
        StaRunner.Run(() =>
        {
            using var service = new HotkeyService();
            Assert.True(service.Register(HotkeyId.SaveFullscreen, new HotkeySetting(false, HotkeyModifiers.None, 0)));
        });
    }

    [Fact]
    public void Strings_EveryPropertyResolves()
    {
        var properties = typeof(Strings).GetProperties().Where(p => p.PropertyType == typeof(string)).ToList();
        Assert.Equal(58, properties.Count);
        foreach (var p in properties)
            Assert.False(string.IsNullOrWhiteSpace((string?)p.GetValue(null)), p.Name);
    }

    [Fact]
    public void Strings_TrayTooltipFormatsHotkey()
    {
        Assert.Equal("Pulsa Impr Pant para capturar", string.Format(Strings.TrayTooltip, "Impr Pant"));
        Assert.Equal("Captura copiada al portapapeles", Strings.ToastCopied);
    }
}
