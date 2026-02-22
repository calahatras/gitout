using System.Windows;
using System.Windows.Input;

namespace GitOut.Features.Wpf.AttachedProperties;

public static class InputBindings
{
    public static readonly DependencyProperty EnterCommandProperty =
        DependencyProperty.RegisterAttached(
            "EnterCommand",
            typeof(ICommand),
            typeof(InputBindings),
            new PropertyMetadata(null, OnEnterCommandChanged)
        );

    public static ICommand GetEnterCommand(DependencyObject obj) =>
        (ICommand)obj.GetValue(EnterCommandProperty);

    public static void SetEnterCommand(DependencyObject obj, ICommand value) =>
        obj.SetValue(EnterCommandProperty, value);

    private static void OnEnterCommandChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is UIElement element)
        {
            if (e.NewValue is ICommand)
            {
                element.KeyDown += Element_KeyDown;
            }
            else
            {
                element.KeyDown -= Element_KeyDown;
            }
        }
    }

    private static void Element_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is DependencyObject d)
        {
            ICommand command = GetEnterCommand(d);
            if (command?.CanExecute(null) == true)
            {
                command.Execute(null);
                e.Handled = true;
            }
        }
    }
}
