using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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
                FocusChild(control.FileTree.ItemContainerGenerator, e.NewValue);
            }

            bool FocusChild(ItemContainerGenerator generator, object child)
            {
                DependencyObject container = generator.ContainerFromItem(child);
                if (container is TreeViewItem treeViewItem)
                {
                    treeViewItem.Focus();
                    return true;
                }

                foreach (object? item in generator.Items)
                {
                    DependencyObject childContainer = generator.ContainerFromItem(item);
                    if (childContainer is TreeViewItem childTreeViewItem)
                    {
                        if (childTreeViewItem.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
                        {
                            if (FocusChild(childTreeViewItem.ItemContainerGenerator, child))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            childTreeViewItem.ItemContainerGenerator.StatusChanged += OnContainerStatusChanged;
                        }
                    }
                }
                return false;
            }

            void OnContainerStatusChanged(object? sender, EventArgs args)
            {
                if (sender is ItemContainerGenerator generator && generator.Status == GeneratorStatus.ContainersGenerated)
                {
                    FocusChild(generator, e.NewValue);
                    generator.StatusChanged -= OnContainerStatusChanged;
                }
            }
        }
    }
}
