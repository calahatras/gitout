using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
        new PropertyMetadata(null)
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

    public static readonly DependencyProperty GroupByPathProperty = DependencyProperty.Register(
        nameof(GroupByPath),
        typeof(string),
        typeof(Autocomplete),
        new PropertyMetadata(null, OnGroupingChanged)
    );

    public string GroupByPath
    {
        get => (string)GetValue(GroupByPathProperty);
        set => SetValue(GroupByPathProperty, value);
    }

    public static readonly DependencyProperty GroupHeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(GroupHeaderTemplate),
            typeof(DataTemplate),
            typeof(Autocomplete),
            new PropertyMetadata(null)
        );

    public DataTemplate GroupHeaderTemplate
    {
        get => (DataTemplate)GetValue(GroupHeaderTemplateProperty);
        set => SetValue(GroupHeaderTemplateProperty, value);
    }

    public static readonly DependencyProperty GroupHeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(GroupHeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(Autocomplete),
            new PropertyMetadata(null)
        );

    public DataTemplateSelector GroupHeaderTemplateSelector
    {
        get => (DataTemplateSelector)GetValue(GroupHeaderTemplateSelectorProperty);
        set => SetValue(GroupHeaderTemplateSelectorProperty, value);
    }
    public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(
        nameof(View),
        typeof(ICollectionView),
        typeof(Autocomplete),
        new PropertyMetadata(null)
    );

    public ICollectionView? View
    {
        get => (ICollectionView?)GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

    public static readonly DependencyProperty IsGroupedProperty = DependencyProperty.Register(
        nameof(IsGrouped),
        typeof(bool),
        typeof(Autocomplete),
        new PropertyMetadata(false, OnGroupingChanged)
    );

    public bool IsGrouped
    {
        get => (bool)GetValue(IsGroupedProperty);
        set => SetValue(IsGroupedProperty, value);
    }

    private readonly CollectionViewSource viewSource = new();
    private ILazyAsyncEnumerable<object, RelativeDirectoryPath>? deferredSource;

    public Autocomplete()
    {
        InitializeComponent();
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

            Type type = viewSource.Source.GetType();
            Type? iReadOnlyCollection = type.GetInterfaces()
                .FirstOrDefault(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)
                );

            int count = 0;
            if (iReadOnlyCollection != null)
            {
                PropertyInfo? countProp = iReadOnlyCollection.GetProperty("Count");
                if (countProp?.GetValue(viewSource.Source) is int roCount)
                {
                    count = roCount;
                }
            }
            if (SelectedIndex < count - 1)
            {
                ++SelectedIndex;
            }
            view.ScrollIntoView(view.SelectedItem);
        });
        viewSource.Filter += OnFilterItem;
        IsVisibleChanged += OnVisibleChanged;
    }

    private void OnFilterItem(object sender, FilterEventArgs e) =>
        e.Accepted = QueryMatcher is null
            ? (e.Item.ToString() is string vl)
                && vl.Contains(SearchQuery, StringComparison.InvariantCultureIgnoreCase)
            : (bool)
                QueryMatcher.Convert(
                    e.Item,
                    typeof(bool),
                    SearchQuery,
                    CultureInfo.InvariantCulture
                );

    public ICommand OpenRecordCommand { get; }

    public ICommand DecreaseSelectionIndexCommand { get; }
    public ICommand IncreaseSelectionIndexCommand { get; }

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
        viewSource.Source = items;
        if (items is ILazyAsyncEnumerable<object, RelativeDirectoryPath> lazy)
        {
            deferredSource = lazy;
        }
        viewSource.GroupDescriptions.Clear();
        if (IsGrouped && !string.IsNullOrWhiteSpace(GroupByPath))
        {
            viewSource.GroupDescriptions.Add(new PropertyGroupDescription(GroupByPath));
        }
        View = viewSource.View;
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        viewSource.View.Refresh();

    private static void OnSearchQueryChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e
    )
    {
        if (d is Autocomplete control && control.View is not null)
        {
            control.View.Refresh();
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

    private static void OnGroupingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (Autocomplete)d;
        control.UpdateLocalView(control.ItemsSource);
    }
}
