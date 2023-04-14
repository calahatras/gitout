using System.Windows.Controls;

namespace GitOut.Features.Text;

public partial class TextPromptPage : UserControl
{
    public TextPromptPage(
        TextPromptViewModel viewModel
    )
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
