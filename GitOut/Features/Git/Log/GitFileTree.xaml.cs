using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace GitOut.Features.Git.Log
{
    public partial class GitFileTree : UserControl
    {
        public static readonly DependencyProperty RootFilesProperty = DependencyProperty.Register(
            nameof(RootFiles),
            typeof(INotifyCollectionChanged),
            typeof(GitFileTree)
        );
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(GitFileTree),
            new PropertyMetadata(null, OnSelectedItemChanged)
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

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GitFileTree control)
            {
                DependencyObject container = control.FileTree.ItemContainerGenerator.ContainerFromItem(e.NewValue);
                if (container is TreeViewItem child)
                {
                    child.Focus();
                }
            }
        }
    }
}
