using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GitOut.Features.Git.Stage
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is GitModifiedStatusType status
            ? status switch
            {
                GitModifiedStatusType.Untracked => (Brush)Application.Current.Resources["Untracked"],
                GitModifiedStatusType.Added => (Brush)Application.Current.Resources["Added"],
                GitModifiedStatusType.Deleted => (Brush)Application.Current.Resources["Removed"],
                GitModifiedStatusType.Modified => (Brush)Application.Current.Resources["Changed"],
                _ => Brushes.White
            }
            : DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
