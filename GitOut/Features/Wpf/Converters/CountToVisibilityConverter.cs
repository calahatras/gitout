using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    public sealed class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
            {
                throw new InvalidOperationException("The target type must be of type Visibility");
            }
            int count = int.Parse(value.ToString());
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => DependencyProperty.UnsetValue;
    }
}
