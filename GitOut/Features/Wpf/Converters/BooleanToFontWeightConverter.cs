using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    public sealed class BooleanToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(FontWeight))
            {
                throw new ArgumentException("Can only convert from bool to font weight", nameof(targetType));
            }
            if (!(value is bool val))
            {
                throw new ArgumentException("Value is not bool", nameof(value));
            }
            return FontWeight.FromOpenTypeWeight(val ? 600 : 400);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => DependencyProperty.UnsetValue;
    }
}
