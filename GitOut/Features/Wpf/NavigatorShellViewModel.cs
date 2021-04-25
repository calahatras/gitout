using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Settings;

namespace GitOut.Features.Wpf
{
    public class NavigatorShellViewModel : INotifyPropertyChanged
    {
        private string? title;
        private ContentControl? content;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public NavigatorShellViewModel() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public NavigatorShellViewModel(
            INavigationService navigation,
            ISnackbarService snack,
            ITitleService titleService
        )
        {
            navigation.NavigationRequested += (sender, args) => Content = args.Control;
            titleService.TitleChanged += (sender, args) => Title = args.Title;

            var snacks = new ObservableCollection<Snack>();
            Snacks = CollectionViewSource.GetDefaultView(snacks);
            Snacks.SortDescriptions.Add(new SortDescription("DateAddedUtc", ListSortDirection.Ascending));
            snack.SnackReceived += (sender, args) => ShowSnack(args.Snack, snacks);

            OpenSettingsCommand = new NavigateLocalCommand<object>(
                navigation,
                typeof(SettingsPage).FullName!,
                null,
                _ => navigation.CurrentPage != typeof(SettingsPage).FullName
            );
        }

        public ICommand OpenSettingsCommand { get; }

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

        public ICollectionView Snacks { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetProperty<T>(ref T prop, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!ReferenceEquals(prop, value))
            {
                prop = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private static void ShowSnack(Snack snack, ICollection<Snack> snackStack)
        {
            Application.Current.Dispatcher.Invoke(() => snackStack.Add(snack));
            snack.Canceled.Register(() => Application.Current.Dispatcher.Invoke(() => snackStack.Remove(snack)));
        }
    }
}
