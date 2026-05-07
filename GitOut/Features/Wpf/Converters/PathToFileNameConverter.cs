using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Wpf.Converters;

[ValueConversion(typeof(object), typeof(string))]
public class PathToFileNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? path = value?.ToString();
        return path is not null
            ? Path.GetFileName(
                path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            )
            : DependencyProperty.UnsetValue;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    ) => DependencyProperty.UnsetValue;
}
