using System.Collections.Specialized;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace GitOut.Features.Wpf.DragDrop
{
    public static class DragDropBehavior
    {
        public static readonly DependencyProperty DropCommandProperty = DependencyProperty.RegisterAttached(
            "DropCommand",
            typeof(ICommand),
            typeof(DragDropBehavior),
            new PropertyMetadata(OnCommandChanged)
        );

        public static readonly DependencyProperty UseAdornerProperty = DependencyProperty.RegisterAttached(
            "UseAdorner",
            typeof(bool),
            typeof(DragDropBehavior),
            new PropertyMetadata(OnUseAdornerChanged)
        );

        public static readonly DependencyProperty AdornerStrokeBrushProperty = DependencyProperty.RegisterAttached(
            "AdornerStrokeBrush",
            typeof(Brush),
            typeof(DragDropBehavior),
            new PropertyMetadata(Brushes.White)
        );

        public static bool GetUseAdorner(DependencyObject obj) => (bool)obj.GetValue(UseAdornerProperty);
        public static ICommand GetDropCommand(DependencyObject obj) => (ICommand)obj.GetValue(DropCommandProperty);
        public static Brush GetAdornerStrokeBrush(DependencyObject obj) => (Brush)obj.GetValue(AdornerStrokeBrushProperty);

        public static void SetUseAdorner(DependencyObject obj, bool value) => obj.SetValue(UseAdornerProperty, value);
        public static void SetDropCommand(DependencyObject obj, ICommand value) => obj.SetValue(DropCommandProperty, value);
        public static void SetAdornerStrokeBrush(DependencyObject obj, Brush value) => obj.SetValue(AdornerStrokeBrushProperty, value);

        private static void OnUseAdornerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                bool useAdorner = (bool)e.NewValue;
                if (useAdorner)
                {
                    var layer = AdornerLayer.GetAdornerLayer(element);
                    layer?.Add(new DropAdorner(element, data => HasDirectory(data) ? "Drop folder here" : "Only accepts folders"));
                }
            }
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                element.Drop -= OnDrop;
                element.Drop += OnDrop;
            }
        }

        private static void OnDrop(object sender, DragEventArgs e)
        {
            if (sender is FrameworkElement element
                && HasDirectory(e.Data)
                && GetDropCommand(element) is ICommand command
                && command.CanExecute(e.Data)
            )
            {
                command.Execute(e.Data);
            }
        }

        private static bool HasDirectory(IDataObject dataObject)
        {
            if (dataObject is not DataObject data)
            {
                return false;
            }

            StringCollection fileDropList = data.GetFileDropList();
            if (fileDropList.Count == 0)
            {
                return false;
            }

            string? firstFile = fileDropList[0];
            if (firstFile is null)
            {
                return false;
            }

            FileAttributes attributes = File.GetAttributes(firstFile);
            return attributes.HasFlag(FileAttributes.Directory);
        }
    }
}
