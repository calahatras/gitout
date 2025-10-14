using System;
using System.Globalization;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters
{
    public sealed class EnumToBooleanConverter : IValueConverter
    {
        public object? Convert(
            object? value,
            Type targetType,
            object parameter,
            CultureInfo culture
        ) =>
            targetType != typeof(bool?)
                ? throw new InvalidOperationException(
                    "The target type must be of type nullable bool"
                )
                : value?.ToString()?.Equals(parameter);

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
}
