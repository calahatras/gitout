using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
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
    public sealed class GeneralSettingsViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IWritableStorage storage;
        private readonly IDisposable unsubscribeOptions;
        private bool useTransparentBackground = true;
        private bool trimLineEndings;
        private bool showSpacesAsDots;
        private string tabTransformText;

        public GeneralSettingsViewModel(
            ISnackbarService snacks,
            IThemeService themes,
            IGitRepositoryStorage repositories,
            IGitRepositoryFactory gitFactory,
            IOptionsMonitor<GitStageOptions> stageOptions,
            IWritableStorage storage
        )
        {
            this.storage = storage;
            var validPaths = new ObservableCollection<ValidGitRepositoryPathViewModel>();
            ValidRepositoryPaths = CollectionViewSource.GetDefaultView(validPaths);

            OpenSettingsFolderCommand = new NavigateExternalCommand(new Uri("file://" + Directory.GetParent(SettingsOptions.GetSettingsPath())));
            SearchRootFolderCommand = new NotNullCallbackCommand<string>(
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
            AddRepositoryCommand = new NotNullCallbackCommand<ValidGitRepositoryPathViewModel>(
                repository =>
                {
                    IGitRepository localrepo = gitFactory.Create(DirectoryPath.Create(repository.WorkingDirectory));
                    repositories.Add(localrepo);
                    snacks.Show("Added repository");
                });
            ChangeThemeCommand = new CallbackCommand<ThemePaletteViewModel>(
                theme =>
                {
                    if (theme is null)
                    {
                        return;
                    }

                    themes.ChangeTheme(theme);
                    snacks.ShowSuccess($"Changed theme to {theme.Name}");
                });

            trimLineEndings = stageOptions.CurrentValue.TrimLineEndings;
            tabTransformText = stageOptions.CurrentValue.TabTransformText;
            showSpacesAsDots = stageOptions.CurrentValue.ShowSpacesAsDots;
            unsubscribeOptions = stageOptions.OnChange(value =>
            {
                SetProperty(ref trimLineEndings, value.TrimLineEndings);
                SetProperty(ref showSpacesAsDots, value.ShowSpacesAsDots);
            });
        }

        public ICollectionView ValidRepositoryPaths { get; }

        public bool UseTransparentBackground
        {
            get => useTransparentBackground;
            set
            {
                if (SetProperty(ref useTransparentBackground, value))
                {
                    Application.Current.Resources["MaterialBackgroundBackground"] = value
                        ? new SolidColorBrush(Color.FromArgb(98, 48, 48, 48))
                        : new SolidColorBrush(Color.FromArgb(255, 48, 48, 48));
                }
            }
        }

        public bool ShowSpacesAsDots
        {
            get => showSpacesAsDots;
            set
            {
                if (SetProperty(ref showSpacesAsDots, value))
                {
                    PersistStorage();
                }
            }
        }

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

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Dispose() => unsubscribeOptions.Dispose();

        private void PersistStorage() => storage.Write(GitStageOptions.SectionKey, new GitStageOptions
        {
            ShowSpacesAsDots = showSpacesAsDots,
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
