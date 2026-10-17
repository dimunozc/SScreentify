using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SScreentify.App.Overlay;
using SScreentify.Core.Annotations;

namespace SScreentify.App.Ui;

/// <summary>Secondary bar shown while the Censor tool is active: the style for new censor boxes, plus the safety note.</summary>
public sealed class CensorBar : UserControl
{
    private readonly Dictionary<CensorStyle, ToggleButton> _styles = new();
    private readonly TextBlock _note;
    private CaptureSession? _session;

    public CensorBar()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (style, label) in new[]
                 {
                     (CensorStyle.Pixelate, Strings.CensorPixelate),
                     (CensorStyle.Blur, Strings.CensorBlur),
                     (CensorStyle.Solid, Strings.CensorSolid),
                 })
        {
            var button = new ToggleButton { Content = label, Padding = new Thickness(10, 0, 10, 0) };
            button.SetResourceReference(StyleProperty, "GlassButton");
            button.Width = double.NaN;
            button.SetResourceReference(FontFamilyProperty, "UiFont");
            button.FontSize = 12;
            button.Click += (_, _) => Choose(style);
            _styles[style] = button;
            row.Children.Add(button);
        }

        _note = new TextBlock
        {
            Text = Strings.CensorNote,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 240,
            FontSize = 11,
            Margin = new Thickness(4, 6, 4, 2),
        };
        _note.SetResourceReference(TextBlock.ForegroundProperty, "MutedTextBrush");
        _note.SetResourceReference(TextBlock.FontFamilyProperty, "UiFont");

        var stack = new StackPanel();
        stack.Children.Add(row);
        stack.Children.Add(_note);
        Panel = new GlassPanel { Content = stack };
        Content = Panel;
    }

    public GlassPanel Panel { get; }

    public string Note => _note.Text;

    public CaptureSession? Session
    {
        get => _session;
        set
        {
            if (_session is not null) _session.StyleChanged -= Sync;
            _session = value;
            if (_session is not null) _session.StyleChanged += Sync;
            Sync();
        }
    }

    public ToggleButton ButtonFor(CensorStyle style) => _styles[style];

    private void Choose(CensorStyle style)
    {
        if (_session is not null) _session.CensorStyle = style;
        Sync();
    }

    private void Sync()
    {
        foreach (var (style, button) in _styles)
            button.IsChecked = _session?.CensorStyle == style;
    }
}
