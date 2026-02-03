using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (targetType != typeof(Visibility))
        {
            throw new InvalidOperationException("The target type must be of type Visibility");
        }
        bool visible = (bool)value;
        bool invert = parameter is not null && bool.Parse((string)parameter);
        return invert ^ visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    ) =>
        targetType != typeof(bool)
            ? throw new InvalidOperationException("The target type must be of type Boolean")
            : (object)((Visibility)value == Visibility.Visible);
}
