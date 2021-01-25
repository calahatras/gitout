using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace GitOut.Features.Wpf.AttachedProperties
{
    public class BindableSelectedItems : DependencyObject
    {
        public static IList GetSelectedItems(DependencyObject obj) => (IList)obj.GetValue(SelectedItemsProperty);

        public static void SetSelectedItems(DependencyObject obj, IList value) => obj.SetValue(SelectedItemsProperty, value);

        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.RegisterAttached(
            "SelectedItems",
            typeof(IList),
            typeof(BindableSelectedItems),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSelectedItemsChanged)
        );

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Selector selector)
            {
                selector.SelectionChanged += OnViewSelectionChanged;
            }
        }

        private static void OnViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(sender is ListBox listBox))
            {
                return;
            }

            IList list = GetSelectedItems(listBox);
            IEnumerable<object>? toRemove = list.OfType<object>().Where(x => !listBox.SelectedItems.Contains(x)).ToList();
            foreach (object? item in toRemove)
            {
                list.Remove(item);
            }

            foreach (object? item in listBox.SelectedItems)
            {
                if (item is null || list.Contains(item))
                {
                    continue;
                }

                list.Add(item);
            }
        }
    }
}
