using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace GitOut.Features.Git.Log.Converters
{
    public class TreeToMarginConverter : IValueConverter
    {
        public const int Distance = 20;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is GitTreeEvent gitTreeEvent))
            {
                return DependencyProperty.UnsetValue;
            }

            int maxIndex = gitTreeEvent.Nodes
                .Select(n => new [] { n.Top?.Up ?? 0, n.Top?.Down ?? 0, n.Bottom?.Down ?? 0})
                .SelectMany(layers => layers)
                .Max();

            return new Thickness(10 + (maxIndex + 1) * Distance, 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
