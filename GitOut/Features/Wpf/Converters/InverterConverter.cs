using System;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    public class InverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double result = double.Parse(value.ToString());
            return -result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double result = double.Parse(value.ToString());
            return -result;
        }
    }
}
