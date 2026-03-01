using System.Windows.Controls;

namespace GitOut.Features.Git.Log;

public partial class CherryPickOptionsPage : UserControl
{
    public CherryPickOptionsPage(CherryPickOptionsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
