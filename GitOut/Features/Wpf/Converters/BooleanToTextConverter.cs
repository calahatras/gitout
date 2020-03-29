using System;
using System.Globalization;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    public sealed class BooleanToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool visible = bool.Parse(value.ToString());
            if (parameter == null)
            {
                return visible ? "On" : "Off";
            }
            string[] s = parameter.ToString().Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
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
