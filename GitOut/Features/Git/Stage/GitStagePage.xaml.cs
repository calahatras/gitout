using System.Windows.Controls;
using System.Windows.Input;

namespace GitOut.Features.Git.Stage
{
    public partial class GitStagePage : UserControl
    {
        public GitStagePage(GitStageViewModel dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;
        }

        private void TunnelEventToParentScroll(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                DocumentScroll.ScrollToHorizontalOffset(DocumentScroll.HorizontalOffset - e.Delta);
            }
            else
            {
                DocumentScroll.ScrollToVerticalOffset(DocumentScroll.VerticalOffset - e.Delta);
            }
        }
    }
}
