using System.Windows.Controls.Primitives;
using SScreentify.App.Overlay;
using SScreentify.App.Ui;
using SScreentify.Core.Annotations;
using SScreentify.Core.Geometry;

namespace SScreentify.App.Tests;

public class CensorBarTests
{
    [Fact]
    public void ClickingStyle_SetsSessionStyleAndChecksOnlyThatButton()
    {
        StaRunner.Run(() =>
        {
            var session = ToolbarTests.NewSession();
            var bar = new CensorBar { Session = session };
            Assert.True(bar.ButtonFor(CensorStyle.Pixelate).IsChecked);

            bar.ButtonFor(CensorStyle.Solid).RaiseEvent(new System.Windows.RoutedEventArgs(ButtonBase.ClickEvent));

            Assert.Equal(CensorStyle.Solid, session.CensorStyle);
            Assert.True(bar.ButtonFor(CensorStyle.Solid).IsChecked);
            Assert.False(bar.ButtonFor(CensorStyle.Pixelate).IsChecked);
            Assert.Equal(Strings.CensorNote, bar.Note);
        });
    }

    [Fact]
    public void NewCensorUsesCurrentStyle_ExistingOnesKeepTheirs()
    {
        var session = ToolbarTests.NewSession(200, 200);
        session.Tool = ToolKind.Censor;
        var tools = new ToolController(session);
        tools.OnMouseDown(new PointD(10, 10), System.Windows.Input.ModifierKeys.None);
        tools.OnMouseUp(new PointD(60, 40), System.Windows.Input.ModifierKeys.None);
        session.CensorStyle = CensorStyle.Blur;
        tools.OnMouseDown(new PointD(100, 100), System.Windows.Input.ModifierKeys.None);
        tools.OnMouseUp(new PointD(150, 140), System.Windows.Input.ModifierKeys.None);

        var styles = session.Document.Items.Cast<CensorAnnotation>().Select(c => c.Style).ToArray();
        Assert.Equal(new[] { CensorStyle.Pixelate, CensorStyle.Blur }, styles);
    }
}
