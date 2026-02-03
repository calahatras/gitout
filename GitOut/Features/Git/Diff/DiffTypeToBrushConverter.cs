using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GitOut.Features.Git.Diff;

public class DiffTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is GitDiffType status
            ? status switch
            {
                GitDiffType.Unmerged => (Brush)Application.Current.Resources["Untracked"],
                GitDiffType.Create => (Brush)Application.Current.Resources["Added"],
                GitDiffType.Delete => (Brush)Application.Current.Resources["Removed"],
                GitDiffType.InPlaceEdit or GitDiffType.RenameEdit => (Brush)
                    Application.Current.Resources["Changed"],
                _ => Brushes.White,
            }
            : DependencyProperty.UnsetValue;

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    ) => Binding.DoNothing;
}
