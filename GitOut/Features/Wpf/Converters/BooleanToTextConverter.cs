using System;
using System.Globalization;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    public sealed class BooleanToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? str = value.ToString();
            bool visible = str != null && bool.Parse(str);
            if (parameter == null)
            {
                return visible ? "On" : "Off";
            }
            string? serializedParameter = parameter.ToString();
            if (serializedParameter == null)
            {
                throw new ArgumentException("parameter may not be null", nameof(parameter));
            }
            string[] s = serializedParameter.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            return visible ? s[0] : s[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
            {
                throw new InvalidOperationException("The target type must be of type Boolean");
            }
            return value.ToString() == "On";
        }
    }
}
