using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters;

/// <summary>
/// Compares two bound values for equality and returns a boolean value.
/// In reverse, setting to false clears the second value.
/// </summary>
public sealed class EqualityToBooleanConverter : IMultiValueConverter
{
    public object Convert(
        object[] values,
        Type targetType,
        object parameter,
        CultureInfo culture
    )
    {
        if (values is null
            || values.Length < 2
            || values[0] is null
            || values[1] is null
            || values[0] == DependencyProperty.UnsetValue
            || values[1] == DependencyProperty.UnsetValue)
        {
            return false;
        }

        string? first = values[0] is CollectionViewGroup group ? group.Name?.ToString() : values[0].ToString();
        string? second = values[1].ToString();

        return string.Equals(first, second, StringComparison.Ordinal);
    }

    public object[] ConvertBack(
        object value,
        Type[] targetTypes,
        object parameter,
        CultureInfo culture
    ) => value is bool b && !b
        ? [Binding.DoNothing, null!]
        : [Binding.DoNothing, Binding.DoNothing];
}
