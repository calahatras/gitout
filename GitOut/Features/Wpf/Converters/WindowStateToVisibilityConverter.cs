using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    public sealed class WindowStateToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
            {
                throw new InvalidOperationException("The target type must be of type Visibility");
            }
            var actual = (WindowState)value;
            var expected = (WindowState)Enum.Parse(typeof(WindowState), (string)parameter);
            return actual == expected ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
            {
                throw new InvalidOperationException("The target type must be of type Boolean");
            }
            return (Visibility)value == Visibility.Visible;
        }
    }
}
