using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace GitOut.Features.Git.Log
{
    public partial class GitFileTree : UserControl
    {
        public static readonly DependencyProperty RootFilesProperty = DependencyProperty.Register(
            "RootFiles",
            typeof(INotifyCollectionChanged),
            typeof(GitFileTree)
        );
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            "SelectedItem",
            typeof(object),
            typeof(GitFileTree)
        );

        public GitFileTree()
        {
            InitializeComponent();
            FileTree.SelectedItemChanged += (sender, args) => SelectedItem = args.NewValue;
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public INotifyCollectionChanged RootFiles
        {
            get => (INotifyCollectionChanged)GetValue(RootFilesProperty);
            set => SetValue(RootFilesProperty, value);
        }
    }
}
