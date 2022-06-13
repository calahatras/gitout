using System.Windows.Controls;

namespace GitOut.Features.ReleaseNotes
{
    public partial class ReleaseNotesPage : UserControl
    {
        public ReleaseNotesPage(
            ReleaseNotesViewModel viewModel
        )
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
