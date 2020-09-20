using System;
using System.Globalization;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BooleanInverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
            {
                throw new ArgumentException("Can only convert from bool to bool", nameof(targetType));
            }
            if (!(value is bool val))
            {
                throw new ArgumentException("Value is not bool", nameof(value));
            }
            return !val;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Convert(value, targetType, parameter, culture);
    }
}
