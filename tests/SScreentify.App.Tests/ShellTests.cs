using System.Windows.Interop;
using SScreentify.App.Native;
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
    public void HotkeyService_PrintScreen_IsInterceptedEvenWhenAlreadyRegisteredElsewhere()
    {
        StaRunner.Run(() =>
        {
            // Simulates Windows 11 reserving the key: another window already owns the RegisterHotKey registration.
            var printScreen = new HotkeySetting(true, HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift, HotkeyRules.VkSnapshot);
            using var owner = new HwndSource(new HwndSourceParameters("taken"));
            Assert.True(User32.RegisterHotKey(owner.Handle, 99, (uint)printScreen.Modifiers, (uint)printScreen.VirtualKey));
            try
            {
                using var service = new HotkeyService();
                Assert.True(service.Register(HotkeyId.Main, printScreen));
                Assert.True(service.Register(HotkeyId.Main, printScreen)); // re-registering replaces the binding
                service.Unregister(HotkeyId.Main);
            }
            finally
            {
                User32.UnregisterHotKey(owner.Handle, 99);
            }
        });
    }

    [Fact]
    public void Strings_EveryPropertyResolves()
    {
        var properties = typeof(Strings).GetProperties().Where(p => p.PropertyType == typeof(string)).ToList();
        Assert.Equal(60, properties.Count);
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
