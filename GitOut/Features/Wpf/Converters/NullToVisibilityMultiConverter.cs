using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    public class NullToVisibilityMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) => values.Any(v => v is not null) ? Visibility.Visible : Visibility.Collapsed;
        public object[]? ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
}
