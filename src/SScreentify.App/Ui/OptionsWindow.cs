using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Win32;
using SScreentify.App.Services;
using SScreentify.Core.Hotkeys;
using SScreentify.Core.Settings;

namespace SScreentify.App.Ui;

/// <summary>General, hotkey and format settings. Result is null when cancelled.</summary>
public sealed class OptionsWindow : DarkWindow
{
    private static readonly Brush FieldBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x2B, 0x38));
    private readonly AppSettings _original;
    private readonly CheckBox _notifications, _cursor, _keepSelection, _startWithWindows, _mainEnabled, _fullEnabled;
    private readonly HotkeyBox _main, _full;
    private readonly Dictionary<ImageFormat, RadioButton> _formats = new();
    private readonly Dictionary<LanguagePreference, RadioButton> _languages = new();
    private readonly Slider _quality;
    private readonly FrameworkElement _qualityRow;
    private readonly TextBox _folder;

    public OptionsWindow(AppSettings current)
    {
        _original = current;
        Title = Strings.OptionsTitle;
        Width = 560;

        _notifications = Check(Strings.OptShowNotifications, current.ShowNotifications);
        _cursor = Check(Strings.OptCaptureCursor, current.CaptureCursor);
        _keepSelection = Check(Strings.OptKeepSelection, current.KeepSelection);
        _startWithWindows = Check(Strings.OptStartWithWindows, current.StartWithWindows);
        // Language names stay in their own language so anyone can find theirs.
        var languageRow = new StackPanel();
        foreach (var (language, label) in new[] { (LanguagePreference.Auto, Strings.LanguageAuto), (LanguagePreference.Spanish, "Español"), (LanguagePreference.English, "English") })
        {
            var radio = new RadioButton { Content = label, GroupName = "language", IsChecked = current.Language == language, Margin = new Thickness(0, 0, 0, 6), Foreground = Palette.Text };
            _languages[language] = radio;
            languageRow.Children.Add(radio);
        }
        var general = Section(_notifications, _cursor, _keepSelection, _startWithWindows, Labeled(Strings.OptLanguage, languageRow));

        _mainEnabled = Check(Strings.HotkeyMain, current.MainHotkey.Enabled);
        _main = Field(new HotkeyBox { Value = current.MainHotkey });
        _fullEnabled = Check(Strings.HotkeySaveFullscreen, current.SaveFullscreenHotkey.Enabled);
        _full = Field(new HotkeyBox { Value = current.SaveFullscreenHotkey });
        var hotkeys = Section(_mainEnabled, _main, _fullEnabled, _full,
            new TextBlock { Text = Strings.HotkeyHint, Foreground = Palette.Icon, FontSize = 12, TextWrapping = TextWrapping.Wrap });

        var formatRow = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (format, label) in new[] { (ImageFormat.Png, "PNG"), (ImageFormat.Jpeg, "JPG"), (ImageFormat.Bmp, "BMP") })
        {
            var radio = new RadioButton { Content = label, GroupName = "format", IsChecked = current.Format == format, Margin = new Thickness(0, 0, 18, 0), Foreground = Palette.Text };
            radio.Checked += (_, _) => UpdateQualityVisibility();
            _formats[format] = radio;
            formatRow.Children.Add(radio);
        }
        _quality = new Slider { Minimum = 1, Maximum = 100, Value = current.JpegQuality, IsSnapToTickEnabled = true, TickFrequency = 1, Width = 260 };
        var qualityValue = new TextBlock { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Width = 30 };
        qualityValue.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(Slider.Value)) { Source = _quality });
        var qualityControl = new StackPanel { Orientation = Orientation.Horizontal };
        qualityControl.Children.Add(_quality);
        qualityControl.Children.Add(qualityValue);
        _qualityRow = Labeled(Strings.OptJpegQuality, qualityControl);

        _folder = Field(new TextBox { Text = current.DefaultFolder ?? AppPaths.DefaultPicturesFolder, IsReadOnly = true, Width = 300 });
        var browse = new Button { Content = Strings.ButtonBrowse, Margin = new Thickness(8, 0, 0, 14), Padding = new Thickness(10, 3, 10, 3), Background = FieldBrush, Foreground = Palette.Text, BorderThickness = new Thickness(0) };
        browse.Click += (_, _) => Browse();
        var folderControl = new StackPanel { Orientation = Orientation.Horizontal };
        folderControl.Children.Add(_folder);
        folderControl.Children.Add(browse);
        var formats = Section(Labeled(Strings.OptFormat, formatRow), _qualityRow, Labeled(Strings.OptDefaultFolder, folderControl));
        UpdateQualityVisibility();

        var pages = new Grid();
        pages.Children.Add(general);
        pages.Children.Add(hotkeys);
        pages.Children.Add(formats);

        var nav = new StackPanel { Margin = new Thickness(12, 16, 0, 16), Width = 120 };
        foreach (var (label, page) in new[] { (Strings.TabGeneral, (FrameworkElement)general), (Strings.TabHotkeys, hotkeys), (Strings.TabFormats, formats) })
        {
            var tab = new ToggleButton { Content = label, Height = 32, Width = double.NaN, HorizontalContentAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 4) };
            tab.SetResourceReference(StyleProperty, "GlassButton");
            tab.Width = double.NaN;
            tab.Click += (_, _) => ShowPage(nav, tab, pages, page);
            nav.Children.Add(tab);
        }
        ShowPage(nav, (ToggleButton)nav.Children[0], pages, general);

        var ok = new Button { Content = Strings.ButtonOk, IsDefault = true, Padding = new Thickness(18, 5, 18, 5), Margin = new Thickness(0, 0, 8, 0), Background = Palette.Accent, Foreground = Brushes.White, BorderThickness = new Thickness(0) };
        ok.Click += (_, _) => Accept();
        var cancel = new Button { Content = Strings.ButtonCancel, IsCancel = true, Padding = new Thickness(18, 5, 18, 5), Background = FieldBrush, Foreground = Palette.Text, BorderThickness = new Thickness(0) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(16) };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);

        var body = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        DockPanel.SetDock(nav, Dock.Left);
        body.Children.Add(buttons);
        body.Children.Add(nav);
        body.Children.Add(pages);
        Content = body;
    }

    public AppSettings? Result { get; private set; }

    private void Accept()
    {
        var main = _main.Value with { Enabled = _mainEnabled.IsChecked == true };
        var full = _full.Value with { Enabled = _fullEnabled.IsChecked == true };
        switch (HotkeyRules.Validate(new[] { main, full }))
        {
            case HotkeyValidationError.Invalid:
                MessageBox.Show(this, Strings.HotkeyInvalid, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            case HotkeyValidationError.Duplicate:
                MessageBox.Show(this, Strings.HotkeyDuplicate, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
        }

        Result = _original with
        {
            ShowNotifications = _notifications.IsChecked == true,
            CaptureCursor = _cursor.IsChecked == true,
            KeepSelection = _keepSelection.IsChecked == true,
            StartWithWindows = _startWithWindows.IsChecked == true,
            MainHotkey = main,
            SaveFullscreenHotkey = full,
            Format = _formats.First(f => f.Value.IsChecked == true).Key,
            JpegQuality = (int)_quality.Value,
            DefaultFolder = _folder.Text == AppPaths.DefaultPicturesFolder ? null : _folder.Text,
            Language = _languages.First(l => l.Value.IsChecked == true).Key,
        };
        DialogResult = true;
    }

    private void Browse()
    {
        var dialog = new OpenFolderDialog { InitialDirectory = Directory.Exists(_folder.Text) ? _folder.Text : AppPaths.DefaultPicturesFolder };
        if (dialog.ShowDialog(this) == true) _folder.Text = dialog.FolderName;
    }

    private void UpdateQualityVisibility()
    {
        if (_qualityRow is not null && _formats.TryGetValue(ImageFormat.Jpeg, out var jpeg))
            _qualityRow.Visibility = jpeg.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void ShowPage(Panel nav, ToggleButton active, Panel pages, FrameworkElement page)
    {
        foreach (ToggleButton tab in nav.Children) tab.IsChecked = tab == active;
        foreach (FrameworkElement p in pages.Children) p.Visibility = p == page ? Visibility.Visible : Visibility.Collapsed;
    }

    private static CheckBox Check(string label, bool value) =>
        new() { Content = label, IsChecked = value, Foreground = Palette.Text, Margin = new Thickness(0, 0, 0, 12) };

    private static T Field<T>(T box) where T : TextBox
    {
        box.Background = FieldBrush;
        box.Foreground = Palette.Text;
        box.BorderBrush = Palette.PanelBorder;
        box.Padding = new Thickness(6, 4, 6, 4);
        box.Margin = new Thickness(0, 0, 0, 14);
        return box;
    }

    private static FrameworkElement Labeled(string label, UIElement control)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 14) };
        stack.Children.Add(new TextBlock { Text = label, Foreground = Palette.Icon, Margin = new Thickness(0, 0, 0, 6) });
        stack.Children.Add(control);
        return stack;
    }

    private static StackPanel Section(params UIElement[] children)
    {
        var stack = new StackPanel { Margin = new Thickness(20, 20, 20, 0) };
        foreach (var child in children) stack.Children.Add(child);
        return stack;
    }
}
