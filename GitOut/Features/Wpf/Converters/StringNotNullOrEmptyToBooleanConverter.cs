using System;
using System.Globalization;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    [ValueConversion(typeof(string), typeof(bool))]
    public class StringNotNullOrEmptyToBooleanConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        ) => value is not string str || !string.IsNullOrEmpty(str);

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        ) => Binding.DoNothing;
    }
}
