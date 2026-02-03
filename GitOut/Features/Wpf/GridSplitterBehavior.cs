using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace GitOut.Features.Wpf;

public static class GridSplitterBehavior
{
    public static int GetFreezeColumn(DependencyObject obj) =>
        (int)obj.GetValue(FreezeColumnProperty);

    public static void SetFreezeColumn(DependencyObject obj, int value) =>
        obj.SetValue(FreezeColumnProperty, value);

    public static int GetFreezeRow(DependencyObject obj) => (int)obj.GetValue(FreezeRowProperty);

    public static void SetFreezeRow(DependencyObject obj, int value) =>
        obj.SetValue(FreezeRowProperty, value);

    public static readonly DependencyProperty FreezeColumnProperty =
        DependencyProperty.RegisterAttached(
            "FreezeColumn",
            typeof(int),
            typeof(Grid),
            new PropertyMetadata(-1, FrozenChanged)
        );

    public static readonly DependencyProperty FreezeRowProperty =
        DependencyProperty.RegisterAttached(
            "FreezeRow",
            typeof(int),
            typeof(Grid),
            new PropertyMetadata(-1, FrozenChanged)
        );

    private static void FrozenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((Grid)d).Loaded += GridLoaded;

    private static void GridLoaded(object sender, RoutedEventArgs e) => HookResize((Grid)sender);

    public static void HookResize(Grid element)
    {
        element.Loaded -= GridLoaded;
        if (DesignerProperties.GetIsInDesignMode(element))
        {
            return;
        }
        if (Window.GetWindow(element) is not NavigatorShell window)
        {
            throw new InvalidOperationException(
                "Parent must be a NavigatorShell for frozen panel to work"
            );
        }
        int column = GetFreezeColumn(element);
        int row = GetFreezeRow(element);
        ColumnDefinition? frozenColumn = null;
        RowDefinition? frozenRow = null;
        if (column >= 0)
        {
            frozenColumn = element.ColumnDefinitions[column];
        }
        if (row >= 0)
        {
            frozenRow = element.RowDefinitions[row];
        }

        bool resizing = false;
        window.Resized += ResizedComplete;
        window.SizeChanged += SizeChanged;
        element.Unloaded += (o, e) =>
        {
            window.SizeChanged -= SizeChanged;
            window.Resized -= ResizedComplete;
        };

        void SizeChanged(object o, EventArgs e)
        {
            if (!resizing)
            {
                if (frozenColumn is not null)
                {
                    frozenColumn.Width = new GridLength(frozenColumn.ActualWidth);
                }
                if (frozenRow is not null)
                {
                    frozenRow.Height = new GridLength(frozenRow.ActualHeight);
                }
            }
            resizing = true;
        }
        ;

        void ResizedComplete(object? o, EventArgs e) => resizing = false;
    }
}
