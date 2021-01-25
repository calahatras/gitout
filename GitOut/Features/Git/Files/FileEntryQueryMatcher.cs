using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Git.Files
{
    public class FileEntryQueryMatcher : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is IGitFileEntryViewModel file && parameter is string query && file.FileName.ToUpperInvariant().Contains(query);
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => DependencyProperty.UnsetValue;
    }
}
