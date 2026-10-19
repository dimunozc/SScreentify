using SScreentify.Core.Hotkeys;

namespace SScreentify.Core.Tests;

public class KeyInterceptorTests
{
    private const int Snap = HotkeyRules.VkSnapshot, F5 = 0x74;
    private static HotkeySetting On(HotkeyModifiers m, int vk) => new(true, m, vk);

    [Fact]
    public void Intercepts_OnlyEnabledPrintScreen()
    {
        Assert.True(KeyInterceptor.Intercepts(On(HotkeyModifiers.None, Snap)));
        Assert.True(KeyInterceptor.Intercepts(On(HotkeyModifiers.Shift, Snap)));
        Assert.False(KeyInterceptor.Intercepts(new HotkeySetting(false, HotkeyModifiers.None, Snap)));
        Assert.False(KeyInterceptor.Intercepts(On(HotkeyModifiers.Control, F5)));
    }

    [Fact]
    public void Press_FiresOnceAndSwallowsDownRepeatAndUp()
    {
        var keys = new KeyInterceptor();
        keys.Bind(1, On(HotkeyModifiers.None, Snap));

        Assert.Equal(new KeyDecision(true, 1), keys.OnKey(Snap, isDown: true, HotkeyModifiers.None));
        Assert.Equal(new KeyDecision(true, null), keys.OnKey(Snap, isDown: true, HotkeyModifiers.None));
        Assert.Equal(new KeyDecision(true, null), keys.OnKey(Snap, isDown: false, HotkeyModifiers.None));
        Assert.Equal(new KeyDecision(true, 1), keys.OnKey(Snap, isDown: true, HotkeyModifiers.None));
    }

    [Fact]
    public void ModifiersMustMatchExactly()
    {
        var keys = new KeyInterceptor();
        keys.Bind(1, On(HotkeyModifiers.None, Snap));
        keys.Bind(2, On(HotkeyModifiers.Shift, Snap));

        Assert.Equal(new KeyDecision(true, 2), keys.OnKey(Snap, isDown: true, HotkeyModifiers.Shift));
        // Releasing Shift before the key still swallows the key's release.
        Assert.Equal(new KeyDecision(true, null), keys.OnKey(Snap, isDown: false, HotkeyModifiers.None));
        Assert.Equal(KeyDecision.Pass, keys.OnKey(Snap, isDown: true, HotkeyModifiers.Control));
        Assert.Equal(KeyDecision.Pass, keys.OnKey(Snap, isDown: false, HotkeyModifiers.Control));
    }

    [Fact]
    public void ReleaseWithoutSeenPress_StillFires()
    {
        var keys = new KeyInterceptor();
        keys.Bind(1, On(HotkeyModifiers.None, Snap));

        Assert.Equal(new KeyDecision(true, 1), keys.OnKey(Snap, isDown: false, HotkeyModifiers.None));
    }

    [Fact]
    public void UnboundOrOtherKeys_Pass()
    {
        var keys = new KeyInterceptor();
        Assert.Equal(KeyDecision.Pass, keys.OnKey(Snap, isDown: true, HotkeyModifiers.None));

        keys.Bind(1, On(HotkeyModifiers.None, Snap));
        Assert.Equal(KeyDecision.Pass, keys.OnKey(F5, isDown: true, HotkeyModifiers.None));

        keys.Unbind(1);
        Assert.Equal(KeyDecision.Pass, keys.OnKey(Snap, isDown: true, HotkeyModifiers.None));
        Assert.Equal(KeyDecision.Pass, keys.OnKey(Snap, isDown: false, HotkeyModifiers.None));
    }

    [Fact]
    public void Bind_RejectsKeysItDoesNotIntercept()
    {
        var keys = new KeyInterceptor();
        Assert.Throws<ArgumentException>(() => keys.Bind(1, On(HotkeyModifiers.Control, F5)));
    }
}
