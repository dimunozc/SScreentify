using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using SScreentify.App.Overlay;
using SScreentify.Core.Annotations;

namespace SScreentify.App.Ui;

/// <summary>Vertical drawing-tools bar. Clicking the active tool again turns it off.</summary>
public sealed class ToolsBar : UserControl
{
    private static readonly (ToolKind Tool, Geometry Icon, Func<string> Tip)[] ToolDefs =
    {
        (ToolKind.Select, Icons.Select, () => Strings.ToolSelect),
        (ToolKind.Pen, Icons.Pen, () => Strings.ToolPen),
        (ToolKind.Line, Icons.Line, () => Strings.ToolLine),
        (ToolKind.Arrow, Icons.Arrow, () => Strings.ToolArrow),
        (ToolKind.Rectangle, Icons.Rectangle, () => Strings.ToolRectangle),
        (ToolKind.Ellipse, Icons.Ellipse, () => Strings.ToolEllipse),
        (ToolKind.Marker, Icons.Marker, () => Strings.ToolMarker),
        (ToolKind.Text, Icons.Text, () => Strings.ToolText),
        (ToolKind.Censor, Icons.Censor, () => Strings.ToolCensor),
    };

    private readonly Dictionary<ToolKind, ToggleButton> _tools = new();
    private readonly Ellipse _swatch = new() { Width = 14, Height = 14, Stroke = new SolidColorBrush(Color.FromArgb(0xB3, 255, 255, 255)), StrokeThickness = 2 };
    private CaptureSession? _session;

    public ToolsBar()
    {
        var stack = new StackPanel { Orientation = Orientation.Vertical };
        foreach (var (tool, icon, tip) in ToolDefs)
        {
            var button = new ToggleButton { Content = Icons.Create(icon), ToolTip = tip(), Tag = tool };
            button.SetResourceReference(StyleProperty, "GlassButton");
            button.Click += (_, _) => Toggle(tool);
            _tools[tool] = button;
            stack.Children.Add(button);
        }

        stack.Children.Add(Separator());
        ColorButton = Button(_swatch, Strings.ToolColor, () => ColorRequested?.Invoke());
        stack.Children.Add(ColorButton);
        stack.Children.Add(Button(Icons.Create(Icons.Undo), Strings.ToolUndo, () => UndoRequested?.Invoke()));
        stack.Children.Add(Button(Icons.Create(Icons.Redo), Strings.ToolRedo, () => RedoRequested?.Invoke()));

        Panel = new GlassPanel { Content = stack };
        Content = Panel;
    }

    public GlassPanel Panel { get; }
    public Button ColorButton { get; }

    public CaptureSession? Session
    {
        get => _session;
        set
        {
            if (_session is not null) { _session.ToolChanged -= Sync; _session.StyleChanged -= Sync; }
            _session = value;
            if (_session is not null) { _session.ToolChanged += Sync; _session.StyleChanged += Sync; }
            Sync();
        }
    }

    public event Action? UndoRequested, RedoRequested, ColorRequested;

    public ToggleButton ButtonFor(ToolKind tool) => _tools[tool];

    private void Toggle(ToolKind tool)
    {
        if (_session is null) return;
        _session.Tool = _session.Tool == tool ? ToolKind.None : tool;
        Sync();
    }

    private void Sync()
    {
        foreach (var (tool, button) in _tools)
            button.IsChecked = _session?.Tool == tool;
        var c = _session?.Color ?? Rgba.Parse("#F43F5E");
        _swatch.Fill = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
    }

    internal static Button Button(object content, string tip, Action onClick, bool primary = false)
    {
        var button = new Button { Content = content, ToolTip = tip };
        button.SetResourceReference(StyleProperty, primary ? "PrimaryGlassButton" : "GlassButton");
        button.Click += (_, _) => onClick();
        return button;
    }

    internal static FrameworkElement Separator(bool vertical = false)
    {
        var line = new Border { Margin = vertical ? new Thickness(3, 4, 3, 4) : new Thickness(4, 3, 4, 3) };
        if (vertical) line.Width = 1; else line.Height = 1;
        line.SetResourceReference(Border.BackgroundProperty, "SeparatorBrush");
        return line;
    }
}
