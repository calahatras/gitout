using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Diagnostics;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Settings;

namespace GitOut.Features.Wpf
{
    public sealed class NavigatorShellViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IDisposable processStreamSubscription;

        private string? title;
        private bool isStatusBarVisible = true;
        private string? statusBarText;
        private ContentControl? content;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public NavigatorShellViewModel() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public NavigatorShellViewModel(
            INavigationService navigation,
            ISnackbarService snack,
            IProcessTelemetryCollector processCollection,
            ITitleService titleService
        )
        {
            navigation.NavigationRequested += (sender, args) => Content = args.Control;
            titleService.TitleChanged += (sender, args) =>
                Title = string.IsNullOrWhiteSpace(args.Title)
                    ? "git out"
                    : $"{args.Title} � git out";

            var snacks = new ObservableCollection<Snack>();
            Snacks = CollectionViewSource.GetDefaultView(snacks);
            Snacks.SortDescriptions.Add(
                new SortDescription("DateAddedUtc", ListSortDirection.Ascending)
            );
            snack.SnackReceived += (sender, args) => ShowSnack(args.Snack, snacks);
            processStreamSubscription = processCollection
                .EventsStream.Select(item =>
                    $"{item.ProcessName} {item.Options.Arguments} finished in {item.Duration.TotalMilliseconds}ms"
                )
                .Subscribe(statusText => StatusBarText = statusText);

            OpenSettingsCommand = new NavigateLocalCommand<object>(
                navigation,
                typeof(SettingsPage).FullName!,
                null,
                _ => navigation.CurrentPage != typeof(SettingsPage).FullName
            );
            ToggleFullScreenCommand = new NotNullCallbackCommand<Window>(window =>
            {
                if (window.WindowStyle == WindowStyle.None)
                {
                    window.WindowStyle = WindowStyle.SingleBorderWindow;
                    window.WindowState = WindowState.Normal;
                }
                else
                {
                    window.WindowStyle = WindowStyle.None;
                    window.WindowState = WindowState.Maximized;
                }
            });
        }

        public ICommand OpenSettingsCommand { get; }
        public ICommand ToggleFullScreenCommand { get; }

        public string? Title
        {
            get => title;
            private set => SetProperty(ref title, value);
        }

        public bool IsStatusBarVisible
        {
            get => isStatusBarVisible;
            set => SetProperty(ref isStatusBarVisible, value);
        }

        public string? StatusBarText
        {
            get => statusBarText;
            private set => SetProperty(ref statusBarText, value);
        }

        public ContentControl? Content
        {
            get => content;
            private set => SetProperty(ref content, value);
        }

        public ICollectionView Snacks { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Dispose() => processStreamSubscription.Dispose();

        private void SetProperty<T>(
            ref T prop,
            T value,
            [CallerMemberName] string? propertyName = null
        )
        {
            if (!ReferenceEquals(prop, value))
            {
                prop = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private static void ShowSnack(Snack snack, ObservableCollection<Snack> snackStack)
        {
            Application.Current.Dispatcher.Invoke(() => snackStack.Add(snack));
            snack.Canceled.Register(() =>
                Application.Current.Dispatcher.Invoke(() => snackStack.Remove(snack))
            );
        }
    }
}
