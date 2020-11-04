using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace GitOut.Features.Git.Log
{
    public partial class GitFileTree : UserControl
    {
        public static readonly DependencyProperty RootFilesProperty = DependencyProperty.Register("RootFiles", typeof(INotifyCollectionChanged), typeof(GitFileTree));

        public GitFileTree() => InitializeComponent();

        public INotifyCollectionChanged RootFiles
        {
            get => (INotifyCollectionChanged)GetValue(RootFilesProperty);
            set => SetValue(RootFilesProperty, value);
        }
    }
}
