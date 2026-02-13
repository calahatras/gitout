using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Git.Log.Converters;

public class TreeToMarginConverter : IValueConverter
{
    public const int Distance = 15;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not GitTreeEvent gitTreeEvent)
        {
            return DependencyProperty.UnsetValue;
        }

        int maxIndex = gitTreeEvent
            .Nodes.SelectMany(n => new[] { n.Top?.Up ?? 0, n.Top?.Down ?? 0, n.Bottom?.Down ?? 0 })
            .Max();

        return new Thickness(10 + ((maxIndex + 1) * Distance), 0, 10, 0);
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture
    ) => Binding.DoNothing;
}
