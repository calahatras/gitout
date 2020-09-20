using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    public class ValueConverterGroup : List<IValueConverter>, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => this.Aggregate(value, (current, converter) =>
        {
            if (Attribute.GetCustomAttribute(converter.GetType(), typeof(ValueConversionAttribute)) is ValueConversionAttribute attribute)
                return converter.Convert(current, attribute.TargetType, parameter, culture);

            return converter.Convert(current, targetType, parameter, culture);
        });

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => DependencyProperty.UnsetValue;
    }
}
