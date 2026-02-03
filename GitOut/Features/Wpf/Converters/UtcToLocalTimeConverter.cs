using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters;

[ValueConversion(typeof(DateTimeOffset), typeof(DateTimeOffset))]
public class UtcToLocalTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not DateTimeOffset utc ? DependencyProperty.UnsetValue : utc.ToLocalTime();

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    ) =>
        value is not DateTimeOffset localtime
            ? DependencyProperty.UnsetValue
            : localtime.ToUniversalTime();
}
