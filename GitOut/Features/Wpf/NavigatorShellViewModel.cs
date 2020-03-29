﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Commands;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Menu;
using GitOut.Features.Navigation;
using Microsoft.Extensions.Hosting;

namespace GitOut.Features.Wpf
{
    public class NavigatorShellViewModel : INotifyPropertyChanged
    {
        private readonly CollectionViewSource menuItems;

        private string? title;
        private ContentControl? content;
        private bool isMenuVisible;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public NavigatorShellViewModel() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public NavigatorShellViewModel(
            IHostApplicationLifetime life,
            INavigationService navigation,
            ISnackbarService snack,
            ITitleService titleService,
            IMenuItemProvider menuItemsProvider
        )
        {
            navigation.NavigationRequested += (sender, args) => Content = args.Control;
            titleService.TitleChanged += (sender, args) => Title = args.Title;

            menuItems = new CollectionViewSource
            {
                Source = menuItemsProvider.GetMenuItems(navigation)
            };

            var snacks = new ObservableCollection<Snack>();
            Snacks = CollectionViewSource.GetDefaultView(snacks);
            Snacks.SortDescriptions.Add(new SortDescription("DateAddedUtc", ListSortDirection.Ascending));
            snack.SnackReceived += (sender, args) => ShowSnackAsync(args.Snack, snacks).ConfigureAwait(false);

            CloseCommand = new CallbackCommand(() => life.StopApplication());
            NavigateBackCommand = new CallbackCommand(
                navigation.Back,
                navigation.CanGoBack
            );
            ToggleMenuCommand = new CallbackCommand(
                () => IsMenuVisible = !IsMenuVisible
            );
        }

        public ICommand MinimizeCommand { get; } = new CallbackCommand<Window>(window => window.WindowState = WindowState.Minimized);
        public ICommand MaximizeCommand { get; } = new CallbackCommand<Window>(window => window.WindowState = WindowState.Maximized);
        public ICommand RestoreCommand { get; } = new CallbackCommand<Window>(window => window.WindowState = WindowState.Normal);
        public ICommand CloseCommand { get; }
        public ICommand NavigateBackCommand { get; }
        public ICommand ToggleMenuCommand { get; }

        public bool IsMenuVisible
        {
            get => isMenuVisible;
            private set => SetProperty(ref isMenuVisible, value);
        }

        public string? Title
        {
            get => title;
            private set => SetProperty(ref title, value);
        }

        public ContentControl? Content
        {
            get => content;
            private set => SetProperty(ref content, value);
        }

        public ICollectionView MenuItems => menuItems.View;
        public ICollectionView Snacks { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetProperty<T>(ref T prop, T value, [CallerMemberName]string? propertyName = null)
        {
            if (!ReferenceEquals(prop, value))
            {
                prop = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private static async Task ShowSnackAsync(Snack snack, ICollection<Snack> snackStack)
        {
            Application.Current.Dispatcher.Invoke(() => snackStack.Add(snack));
            await Task.Delay(snack.Duration).ConfigureAwait(false);
            Application.Current.Dispatcher.Invoke(() => snackStack.Remove(snack));
        }
    }
}
