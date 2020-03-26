using System.Windows;

namespace GitOut
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new GitHistory();
        }
    }
}
