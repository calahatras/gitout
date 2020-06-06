using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Commands;
using GitOut.Features.Git;
using GitOut.Features.Git.Storage;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Themes;
using GitOut.Features.Wpf;

namespace GitOut.Features.Settings
{
    public class SettingsViewModel
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public SettingsViewModel() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public SettingsViewModel(
            ITitleService title,
            INavigationService navigation,
            ISnackbarService snacks,
            IThemeService themes,
            IGitRepositoryStorage storage
        )
        {
            title.Title = "Settings";

            var validPaths = new ObservableCollection<ValidGitRepositoryPathViewModel>();
            ValidRepositoryPaths = CollectionViewSource.GetDefaultView(validPaths);

            MenuItem[] menuItems = new[]
            {
                new MenuItem
                {
                    Command = new CallbackCommand(
                        () => {}
                    ),
                    IconResourceKey = "Cog",
                    Name = "Settings"
                }
            };
            MenuItems = CollectionViewSource.GetDefaultView(menuItems);

            OpenSettingsFolderCommand = new NavigateExternalCommand(new Uri("file://" + Directory.GetParent(SettingsOptions.GetSettingsPath())));
            SearchRootFolderCommand = new CallbackCommand<string>(
                folder =>
                {
                    var info = new DirectoryInfo(folder);
                    if (!info.Exists)
                    {
                        snacks.Show("Folder does not exist");
                        return;
                    }
                    SynchronizationContext sync = SynchronizationContext.Current!;
                    Task.Run(() =>
                    {
                        DirectoryInfo[] gitdirectories = info.GetDirectories(".git", SearchOption.AllDirectories);
                        sync.Post(s =>
                        {
                            snacks.Show($"Found {gitdirectories.Length} repositories");
                            foreach (DirectoryInfo dir in gitdirectories)
                            {
                                validPaths.Add(ValidGitRepositoryPathViewModel.FromGitFolder(dir));
                            }
                        }, null);
                    });
                },
                folder => !string.IsNullOrEmpty(folder)
            );
            AddRepositoryCommand = new CallbackCommand<ValidGitRepositoryPathViewModel>(
                repository =>
                {
                    var localrepo = LocalGitRepository.InitializeFromPath(DirectoryPath.Create(repository.WorkingDirectory));
                    storage.Add(localrepo);
                    snacks.Show("Added repository");
                });
            ChangeThemeCommand = new CallbackCommand<ThemePaletteViewModel>(
                theme =>
                {
                    themes.ChangeTheme(theme);
                    snacks.ShowSuccess($"Changed theme to {theme.Name}");
                });
            NavigateBackCommand = new CallbackCommand(navigation.Back, navigation.CanGoBack);
        }

        public ICollectionView ValidRepositoryPaths { get; }
        public ICollectionView MenuItems { get; }

        public ICommand OpenSettingsFolderCommand { get; }
        public ICommand SearchRootFolderCommand { get; }
        public ICommand AddRepositoryCommand { get; }
        public ICommand ChangeThemeCommand { get; }
        public ICommand NavigateBackCommand { get; }
    }
}
