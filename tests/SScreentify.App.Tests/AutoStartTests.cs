using System.Windows.Input;
using Microsoft.Win32;
using SScreentify.App.Services;
using SScreentify.App.Ui;
using SScreentify.Core.Hotkeys;

namespace SScreentify.App.Tests;

public class AutoStartTests
{
    [Fact]
    public void AutoStart_ApplyTrueThenFalse()
    {
        const string keyPath = @"Software\SScreentify.Tests\Run";
        try
        {
            using var run = Registry.CurrentUser.CreateSubKey(keyPath);
            var autoStart = new AutoStart(run);

            autoStart.Apply(true, @"C:\Apps\SScreentify.exe");
            Assert.Equal("\"C:\\Apps\\SScreentify.exe\"", run.GetValue("SScreentify"));
            Assert.True(autoStart.IsEnabled());

            autoStart.Apply(false, @"C:\Apps\SScreentify.exe");
            Assert.Null(run.GetValue("SScreentify"));
            Assert.False(autoStart.IsEnabled());

            autoStart.Apply(false, @"C:\Apps\SScreentify.exe"); // already absent: no error
        }
        finally
        {
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\SScreentify.Tests", throwOnMissingSubKey: false);
        }
    }

    [Fact]
    public void HotkeyBox_RecordsComboAndBackspaceClears()
    {
        StaRunner.Run(() =>
        {
            var box = new HotkeyBox { Value = new HotkeySetting(true, HotkeyModifiers.None, HotkeyRules.VkSnapshot) };
            Assert.Equal("Impr Pant", box.Text);

            box.Record(Key.F5, ModifierKeys.Control | ModifierKeys.Shift);
            Assert.Equal(new HotkeySetting(true, HotkeyModifiers.Control | HotkeyModifiers.Shift, 0x74), box.Value);
            Assert.Equal("Ctrl + Mayús + F5", box.Text);

            box.Record(Key.LeftCtrl, ModifierKeys.Control);
            Assert.Equal(0x74, box.Value.VirtualKey);

            box.Record(Key.Back, ModifierKeys.None);
            Assert.Equal(0, box.Value.VirtualKey);
        });
    }
}
