using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    [ValueConversion(typeof(DateTimeOffset), typeof(DateTimeOffset))]
    public class UtcToLocalTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTimeOffset utc))
            {
                return DependencyProperty.UnsetValue;
            }

            return utc.ToLocalTime();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DateTimeOffset localtime))
            {
                return DependencyProperty.UnsetValue;
            }

            return localtime.ToUniversalTime();
        }
    }
}
