using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xaml;

namespace GitOut.Features.Wpf.Converters
{
    public class StaticResourceConverter : MarkupExtension, IValueConverter
    {
        private Control? _target;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string resourceKey)
            {
                return _target?.FindResource(resourceKey) ?? Application.Current.FindResource(resourceKey);
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => DependencyProperty.UnsetValue;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var rootObjectProvider = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
            if (rootObjectProvider == null)
            {
                return this;
            }

            _target = rootObjectProvider.RootObject as Control;
            return this;
        }
    }
}
