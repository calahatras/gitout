using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    [ValueConversion(typeof(object), typeof(string))]
    public class ToStringConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        ) => value?.ToString() ?? DependencyProperty.UnsetValue;

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        ) => DependencyProperty.UnsetValue;
    }
}
