using System.Windows.Controls;

namespace SScreentify.App.Ui;

/// <summary>Horizontal actions bar; Copy is the primary action.</summary>
public sealed class ActionsBar : UserControl
{
    public ActionsBar()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(ToolsBar.Button(Icons.Create(Icons.Save), Strings.ActionSave, () => SaveRequested?.Invoke()));
        CopyButton = ToolsBar.Button(Icons.Create(Icons.Copy), Strings.ActionCopy, () => CopyRequested?.Invoke(), primary: true);
        row.Children.Add(CopyButton);
        row.Children.Add(ToolsBar.Button(Icons.Create(Icons.Print), Strings.ActionPrint, () => PrintRequested?.Invoke()));
        row.Children.Add(ToolsBar.Separator(vertical: true));
        row.Children.Add(ToolsBar.Button(Icons.Create(Icons.Close), Strings.ActionClose, () => CloseRequested?.Invoke()));

        Panel = new GlassPanel { Content = row };
        Content = Panel;
    }

    public GlassPanel Panel { get; }
    public Button CopyButton { get; }

    public event Action? CopyRequested, SaveRequested, PrintRequested, CloseRequested;
}
