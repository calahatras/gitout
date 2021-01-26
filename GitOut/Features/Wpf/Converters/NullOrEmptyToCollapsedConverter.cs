using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    public sealed class NullOrEmptyToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => DependencyProperty.UnsetValue;
    }
}
