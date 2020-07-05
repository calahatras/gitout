using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using GitOut.Features.Commands;
using GitOut.Features.Git;
using GitOut.Features.Git.Stage;
using GitOut.Features.Git.Storage;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Storage;
using GitOut.Features.Themes;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Settings
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IWritableStorage storage;
        private bool trimLineEndings;
        private string tabTransformText;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public SettingsViewModel() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public SettingsViewModel(
            ITitleService title,
            INavigationService navigation,
            ISnackbarService snacks,
            IThemeService themes,
            IGitRepositoryStorage repositories,
            IOptionsMonitor<GitStageOptions> stageOptions,
            IWritableStorage storage
        )
        {
            this.storage = storage;
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
                    repositories.Add(localrepo);
                    snacks.Show("Added repository");
                });
            ChangeThemeCommand = new CallbackCommand<ThemePaletteViewModel>(
                theme =>
                {
                    themes.ChangeTheme(theme);
                    snacks.ShowSuccess($"Changed theme to {theme.Name}");
                });
            NavigateBackCommand = new CallbackCommand(navigation.Back, navigation.CanGoBack);

            trimLineEndings = stageOptions.CurrentValue.TrimLineEndings;
            tabTransformText = stageOptions.CurrentValue.TabTransformText;
            stageOptions.OnChange(value => SetProperty(ref trimLineEndings, value.TrimLineEndings));
        }

        public ICollectionView ValidRepositoryPaths { get; }
        public ICollectionView MenuItems { get; }

        public bool TrimLineEndings
        {
            get => trimLineEndings;
            set
            {
                if (SetProperty(ref trimLineEndings, value))
                {
                    PersistStorage();
                }
            }
        }

        public string TabTransformText
        {
            get => tabTransformText;
            set
            {
                if (SetProperty(ref tabTransformText, value))
                {
                    PersistStorage();
                }
            }
        }

        public ICommand OpenSettingsFolderCommand { get; }
        public ICommand SearchRootFolderCommand { get; }
        public ICommand AddRepositoryCommand { get; }
        public ICommand ChangeThemeCommand { get; }
        public ICommand NavigateBackCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void PersistStorage() => storage.Write(GitStageOptions.SectionKey, new GitStageOptions
        {
            TrimLineEndings = trimLineEndings,
            TabTransformText = tabTransformText
        });

        private bool SetProperty<T>(ref T prop, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!ReferenceEquals(prop, value))
            {
                prop = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }
    }
}
