using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Collections;
using GitOut.Features.IO;
using GitOut.Features.Wpf;

namespace GitOut.Features.Text;

public partial class Autocomplete : UserControl
{
    public static readonly DependencyProperty CancelCommandProperty = DependencyProperty.Register(
        nameof(CancelCommand),
        typeof(ICommand),
        typeof(Autocomplete)
    );

    public static readonly DependencyProperty ItemSelectedCommandProperty =
        DependencyProperty.Register(
            nameof(ItemSelectedCommand),
            typeof(ICommand),
            typeof(Autocomplete)
        );

    public static readonly DependencyProperty DropCommandProperty = DependencyProperty.Register(
        nameof(DropCommand),
        typeof(ICommand),
        typeof(Autocomplete)
    );

    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header),
        typeof(string),
        typeof(Autocomplete),
        new PropertyMetadata("Search")
    );

    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
        nameof(SelectedIndex),
        typeof(int),
        typeof(Autocomplete),
        new PropertyMetadata(0)
    );

    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(IEnumerable<object>),
        typeof(Autocomplete),
        new FrameworkPropertyMetadata(null, OnItemsSourceCollectionChanged)
    );

    public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(
        nameof(ItemTemplate),
        typeof(DataTemplate),
        typeof(Autocomplete)
    );

    public static readonly DependencyProperty QueryMatcherProperty = DependencyProperty.Register(
        nameof(QueryMatcher),
        typeof(IValueConverter),
        typeof(Autocomplete),
        new PropertyMetadata(null, OnQueryMatcherChanged)
    );

    public static readonly DependencyProperty SearchQueryProperty = DependencyProperty.Register(
        nameof(SearchQuery),
        typeof(string),
        typeof(Autocomplete),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnSearchQueryChanged
        )
    );

    private readonly object localItemsLock = new();
    private readonly ObservableCollection<object?> localItems = new();
    private readonly ICollectionView localView;

    private ILazyAsyncEnumerable<object, RelativeDirectoryPath>? deferredSource;

    public Autocomplete()
    {
        InitializeComponent();
        BindingOperations.EnableCollectionSynchronization(localItems, localItemsLock);
        localView = CollectionViewSource.GetDefaultView(localItems);
        OpenRecordCommand = new CallbackCommand<object?>(selection =>
        {
            if (ItemSelectedCommand is not null && ItemSelectedCommand.CanExecute(selection))
            {
                ItemSelectedCommand.Execute(selection);
            }
            if (CancelCommand is not null && CancelCommand.CanExecute(null))
            {
                CancelCommand.Execute(null);
            }
        });
        DecreaseSelectionIndexCommand = new CallbackCommand<ListView>(view =>
        {
            if (view is null)
            {
                return;
            }

            if (SelectedIndex > 0)
            {
                --SelectedIndex;
            }
            view.ScrollIntoView(view.SelectedItem);
        });
        IncreaseSelectionIndexCommand = new CallbackCommand<ListView>(view =>
        {
            if (view is null)
            {
                return;
            }

            if (SelectedIndex < localItems.Count - 1)
            {
                ++SelectedIndex;
            }
            view.ScrollIntoView(view.SelectedItem);
        });
        IsVisibleChanged += OnVisibleChanged;
    }

    public ICommand OpenRecordCommand { get; }

    public ICommand DecreaseSelectionIndexCommand { get; }
    public ICommand IncreaseSelectionIndexCommand { get; }

    public ICollectionView Local => localView;

    public ICommand CancelCommand
    {
        get => (ICommand)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public ICommand ItemSelectedCommand
    {
        get => (ICommand)GetValue(ItemSelectedCommandProperty);
        set => SetValue(ItemSelectedCommandProperty, value);
    }

    public ICommand DropCommand
    {
        get => (ICommand)GetValue(DropCommandProperty);
        set => SetValue(DropCommandProperty, value);
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public IEnumerable<object> ItemsSource
    {
        get => (IEnumerable<object>)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public string SearchQuery
    {
        get => (string)GetValue(SearchQueryProperty);
        set => SetValue(SearchQueryProperty, value);
    }

    public IValueConverter QueryMatcher
    {
        get => (IValueConverter)GetValue(QueryMatcherProperty);
        set => SetValue(QueryMatcherProperty, value);
    }

    private async void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool visible && visible)
        {
            if (deferredSource is ILazyAsyncEnumerable<object, RelativeDirectoryPath> lazy)
            {
                await lazy.MaterializeAsync(RelativeDirectoryPath.Root);
            }
            await Dispatcher.BeginInvoke(new Action(() => SearchInput.Focus()));
        }
    }

    private void UpdateLocalView(IEnumerable<object> items)
    {
        if (items is ILazyAsyncEnumerable<object, RelativeDirectoryPath> lazy)
        {
            deferredSource = lazy;
        }
        lock (localItemsLock)
        {
            localItems.Clear();
            foreach (object? item in items)
            {
                localItems.Add(item);
            }
        }
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    lock (localItemsLock)
                    {
                        if (e.NewItems is null)
                        {
                            return;
                        }

                        for (
                            int index = e.NewStartingIndex, i = 0;
                            i < e.NewItems.Count;
                            ++i, ++index
                        )
                        {
                            localItems.Insert(index, e.NewItems[i]);
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Remove:
                {
                    lock (localItemsLock)
                    {
                        if (e.OldItems is null)
                        {
                            return;
                        }

                        foreach (object? item in e.OldItems)
                        {
                            localItems.Remove(item);
                        }
                    }
                }
                break;
            case NotifyCollectionChangedAction.Reset:
                {
                    lock (localItemsLock)
                    {
                        localItems.Clear();
                    }
                }
                break;
            default:
                throw new ArgumentException($"Invalid collection action {e.Action}", nameof(e));
        }
    }

    private static void OnQueryMatcherChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is Autocomplete control)
        {
            control.localView.Filter = item =>
                e.NewValue is not IValueConverter converter
                || control.SearchQuery is null
                || (bool)
                    converter.Convert(
                        item,
                        typeof(bool),
                        control.SearchQuery.ToUpperInvariant(),
                        CultureInfo.CurrentCulture
                    );
        }
    }

    private static void OnSearchQueryChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is Autocomplete control)
        {
            control.localView.Refresh();
        }
    }

    private static void OnItemsSourceCollectionChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is Autocomplete control)
        {
            if (e.NewValue is IEnumerable<object> items)
            {
                control.UpdateLocalView(items);
            }
            if (e.OldValue is INotifyCollectionChanged previous)
            {
                previous.CollectionChanged -= control.OnSourceCollectionChanged;
            }
            if (e.NewValue is INotifyCollectionChanged next)
            {
                next.CollectionChanged += control.OnSourceCollectionChanged;
            }
        }
    }
}
