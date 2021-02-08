using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GitOut.Features.Wpf
{
    public partial class CopyableCommandText : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(CopyableCommandText)
        );

        public static readonly DependencyProperty RouteActionProperty = DependencyProperty.Register(
            nameof(RouteAction),
            typeof(MouseAction),
            typeof(CopyableCommandText),
            new PropertyMetadata(MouseAction.LeftClick)
        );

        public static readonly DependencyProperty RouteCommandProperty = DependencyProperty.Register(
            nameof(RouteCommand),
            typeof(ICommand),
            typeof(CopyableCommandText)
        );

        public static readonly DependencyProperty RouteCommandParameterProperty = DependencyProperty.Register(
            nameof(RouteCommandParameter),
            typeof(object),
            typeof(CopyableCommandText)
        );

        public static readonly DependencyProperty CopyCommandProperty = DependencyProperty.Register(
            nameof(CopyCommand),
            typeof(ICommand),
            typeof(CopyableCommandText)
        );

        public static readonly DependencyProperty CopyCommandParameterProperty = DependencyProperty.Register(
            nameof(CopyCommandParameter),
            typeof(object),
            typeof(CopyableCommandText)
        );
        public static readonly DependencyProperty ButtonVisibilityProperty = DependencyProperty.Register(
            nameof(ButtonVisibility),
            typeof(Visibility),
            typeof(CopyableCommandText)
        );

        public static readonly DependencyProperty TextStyleProperty = DependencyProperty.Register(
            nameof(TextStyle),
            typeof(Style),
            typeof(CopyableCommandText)
        );

        public CopyableCommandText() => InitializeComponent();

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        public MouseAction RouteAction
        {
            get { return (MouseAction)GetValue(RouteActionProperty); }
            set { SetValue(RouteActionProperty, value); }
        }
        public ICommand RouteCommand
        {
            get => (ICommand)GetValue(RouteCommandProperty);
            set => SetValue(RouteCommandProperty, value);
        }
        public object RouteCommandParameter
        {
            get => GetValue(RouteCommandParameterProperty);
            set => SetValue(RouteCommandParameterProperty, value);
        }
        public ICommand CopyCommand
        {
            get => (ICommand)GetValue(CopyCommandProperty);
            set => SetValue(CopyCommandProperty, value);
        }
        public object CopyCommandParameter
        {
            get => GetValue(CopyCommandParameterProperty);
            set => SetValue(CopyCommandParameterProperty, value);
        }
        public Visibility ButtonVisibility
        {
            get => (Visibility)GetValue(VisibilityProperty);
            set => SetValue(ButtonVisibilityProperty, value);
        }
        public Style TextStyle
        {
            get => (Style)GetValue(TextStyleProperty);
            set => SetValue(TextStyleProperty, value);
        }
    }
}
