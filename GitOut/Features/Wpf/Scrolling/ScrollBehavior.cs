using System.Windows;
using System.Windows.Controls;

namespace GitOut.Features.Wpf.Scrolling;

public static class ScrollBehavior
{
    public static readonly DependencyProperty EnsureInViewProperty =
        DependencyProperty.RegisterAttached(
            "EnsureInView",
            typeof(object),
            typeof(ScrollBehavior),
            new PropertyMetadata(OnEnsureInViewChanged)
        );

    public static object GetEnsureInView(DependencyObject obj) =>
        obj.GetValue(EnsureInViewProperty);

    public static void SetEnsureInView(DependencyObject obj, object value) =>
        obj.SetValue(EnsureInViewProperty, value);

    private static void OnEnsureInViewChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is ListBox listBox && e.NewValue is not null)
        {
            listBox.ScrollIntoView(e.NewValue);
        }
    }
}
