using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace SScreentify.App.Ui;

public sealed class AboutWindow : DarkWindow
{
    public AboutWindow()
    {
        Title = Strings.AboutTitle;
        Width = 380;
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        var logo = new Image
        {
            Source = new System.Windows.Media.DrawingImage(BrandMark.AppIcon(48)),
            Width = 48, Height = 48, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Top,
        };
        var text = new StackPanel();
        text.Children.Add(new TextBlock { Text = "SScreentify", FontSize = 20, FontWeight = FontWeights.SemiBold });
        text.Children.Add(new TextBlock { Text = string.Format(Strings.AboutVersion, version), Foreground = Palette.Icon, Margin = new Thickness(0, 2, 0, 10) });
        text.Children.Add(new TextBlock { Text = Strings.AboutText, TextWrapping = TextWrapping.Wrap, Foreground = Palette.Icon });

        var row = new DockPanel { Margin = new Thickness(24) };
        DockPanel.SetDock(logo, Dock.Left);
        row.Children.Add(logo);
        row.Children.Add(text);
        Content = row;
    }
}
