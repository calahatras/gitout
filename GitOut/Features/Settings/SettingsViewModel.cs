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
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Settings
{
    public class SettingsViewModel
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public SettingsViewModel() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public SettingsViewModel(
            ITitleService title,
            ISnackbarService snacks,
            IGitRepositoryStorage storage,
            IOptions<SettingsOptions> options
        )
        {
            title.Title = "Inställningar";

            var validPaths = new ObservableCollection<ValidGitRepositoryPathViewModel>();
            ValidRepositoryPaths = CollectionViewSource.GetDefaultView(validPaths);

            OpenSettingsFolderCommand = new NavigateExternalCommand(new Uri("file://" + options.Value.SettingsFolder));

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
        }

        public ICollectionView ValidRepositoryPaths { get; }
        public ICommand OpenSettingsFolderCommand { get; }
        public ICommand SearchRootFolderCommand { get; }
        public ICommand AddRepositoryCommand { get; }
    }
}
