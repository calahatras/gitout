using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters;

public sealed class EnumToBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        targetType != typeof(bool?) && targetType != typeof(bool)
            ? throw new InvalidOperationException("The target type must be a boolean")
        : value is not null && parameter is string enumValue
            ? enumValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Any(item => Enum.Parse(value.GetType(), item).Equals(value))
        : Enum.Parse(parameter.GetType(), parameter.ToString()!, false).Equals(value);

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    ) =>
        !targetType.IsEnum
            ? throw new InvalidOperationException("The target type must be an enum type")
        : parameter is not string enumValue
            ? throw new InvalidOperationException("parameter must be set to value of enum type")
        : Enum.Parse(targetType, enumValue, false);
}
