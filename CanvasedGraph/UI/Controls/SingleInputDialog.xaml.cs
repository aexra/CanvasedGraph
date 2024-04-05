using Microsoft.UI.Xaml.Controls;

namespace CanvasedGraph.UI.Controls;
public sealed partial class SingleInputDialog : UserControl
{
    public string Placeholder = "Input value";

    public string Input => InputBox.Text;
    public string GetInput() => InputBox.Text;

    public SingleInputDialog()
    {
        this.InitializeComponent();
    }
}