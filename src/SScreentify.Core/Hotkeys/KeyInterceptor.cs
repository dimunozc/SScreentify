namespace SScreentify.Core.Hotkeys;

/// <summary>What to do with one low-level key event: swallow it so no other program sees it, and which binding fires.</summary>
public readonly record struct KeyDecision(bool Swallow, int? Fire)
{
    public static readonly KeyDecision Pass = new(false, null);
}

/// <summary>
/// Hotkeys on Impr Pant are read from the low-level keyboard stream instead of RegisterHotKey, so they keep working
/// when Windows 11 reserves the key for the Snipping Tool. Every event of a matched press (down, auto-repeat, up) is
/// swallowed so the Snipping Tool never sees it, and the binding fires once per press.
/// Thread-safe: bindings change on the UI thread while events arrive on the hook thread.
/// </summary>
public sealed class KeyInterceptor
{
    private readonly object _gate = new();
    private readonly Dictionary<int, HotkeySetting> _bindings = new();
    private int? _heldKey;

    public static bool Intercepts(HotkeySetting s) => s.Enabled && s.VirtualKey == HotkeyRules.VkSnapshot;

    public void Bind(int id, HotkeySetting s)
    {
        if (!Intercepts(s))
            throw new ArgumentException("Only enabled Impr Pant hotkeys are intercepted.", nameof(s));
        lock (_gate) _bindings[id] = s;
    }

    public void Unbind(int id)
    {
        lock (_gate) _bindings.Remove(id);
    }

    public KeyDecision OnKey(int vk, bool isDown, HotkeyModifiers modifiers)
    {
        lock (_gate)
        {
            if (_heldKey == vk)
            {
                if (!isDown) _heldKey = null;
                return new KeyDecision(true, null);
            }

            int? match = null;
            foreach (var (id, s) in _bindings)
                if (s.VirtualKey == vk && s.Modifiers == modifiers) { match = id; break; }
            if (match is null)
                return KeyDecision.Pass;

            // A release whose press was never seen (for example, pressed before the hook started) still counts as one press.
            if (isDown) _heldKey = vk;
            return new KeyDecision(true, match);
        }
    }
}
