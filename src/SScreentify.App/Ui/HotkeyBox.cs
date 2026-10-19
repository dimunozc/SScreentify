using System.Windows.Controls;
using System.Windows.Input;
using SScreentify.Core.Hotkeys;

namespace SScreentify.App.Ui;

/// <summary>Records a key combination when focused. Backspace or Delete clears it.</summary>
public sealed class HotkeyBox : TextBox
{
    private HotkeySetting _value = new(true, HotkeyModifiers.None, 0);

    public HotkeyBox()
    {
        IsReadOnly = true;
        IsReadOnlyCaretVisible = false;
        Cursor = Cursors.Hand;
    }

    public HotkeySetting Value
    {
        get => _value;
        set
        {
            _value = value;
            Text = value.VirtualKey == 0 ? "—" : HotkeyRules.Display(value, UiLanguage.Current);
        }
    }

    public void Record(Key key, ModifierKeys modifiers)
    {
        if (key is Key.Back or Key.Delete && modifiers == ModifierKeys.None)
        {
            Value = _value with { Modifiers = HotkeyModifiers.None, VirtualKey = 0 };
            return;
        }
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        var mods = HotkeyModifiers.None;
        if (modifiers.HasFlag(ModifierKeys.Control)) mods |= HotkeyModifiers.Control;
        if (modifiers.HasFlag(ModifierKeys.Alt)) mods |= HotkeyModifiers.Alt;
        if (modifiers.HasFlag(ModifierKeys.Shift)) mods |= HotkeyModifiers.Shift;
        if (modifiers.HasFlag(ModifierKeys.Windows)) mods |= HotkeyModifiers.Win;
        Value = _value with { Modifiers = mods, VirtualKey = KeyInterop.VirtualKeyFromKey(key) };
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        Record(e.Key == Key.System ? e.SystemKey : e.Key, Keyboard.Modifiers);
        e.Handled = true;
    }

    /// <summary>Print Screen only arrives as a key-up.</summary>
    protected override void OnPreviewKeyUp(KeyEventArgs e)
    {
        if (e.Key == Key.Snapshot) Record(Key.Snapshot, Keyboard.Modifiers);
        e.Handled = true;
    }
}
