using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Git.RepositoryList;

public class RepositoryQueryMatcher : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is IGitRepository repo
        && parameter is string query
        && repo.Name.Contains(query, StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    ) => DependencyProperty.UnsetValue;
}
