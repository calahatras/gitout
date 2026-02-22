using System.Windows;
using System.Windows.Controls;

namespace GitOut.Features.Wpf.AttachedProperties;

public static class ListBoxBehavior
{
    public static readonly DependencyProperty ScrollSelectedIntoViewProperty =
        DependencyProperty.RegisterAttached(
            "ScrollSelectedIntoView",
            typeof(bool),
            typeof(ListBoxBehavior),
            new UIPropertyMetadata(false, OnScrollSelectedIntoViewChanged)
        );

    public static bool GetScrollSelectedIntoView(DependencyObject obj) =>
        (bool)obj.GetValue(ScrollSelectedIntoViewProperty);

    public static void SetScrollSelectedIntoView(DependencyObject obj, bool value) =>
        obj.SetValue(ScrollSelectedIntoViewProperty, value);

    private static void OnScrollSelectedIntoViewChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is ListBox listBox)
        {
            if ((bool)e.NewValue)
            {
                listBox.SelectionChanged += ListBox_SelectionChanged;
                // Also try to scroll initially if there is a selection
                if (listBox.SelectedItem != null)
                {
                    listBox.ScrollIntoView(listBox.SelectedItem);
                }
            }
            else
            {
                listBox.SelectionChanged -= ListBox_SelectionChanged;
            }
        }
    }

    private static void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem != null)
        {
            listBox.ScrollIntoView(listBox.SelectedItem);
        }
    }
}
