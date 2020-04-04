using System;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    public class InverterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string? s = value.ToString();
            if (s == null)
            {
                throw new ArgumentException("value.ToString() may not be null", nameof(value));
            }
            double result = double.Parse(s);
            return -result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string? s = value.ToString();
            if (s == null)
            {
                throw new ArgumentException("value.ToString() may not be null", nameof(value));
            }
            double result = double.Parse(s);
            return -result;
        }
    }
}
