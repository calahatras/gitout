using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using GitOut.Features.Diagnostics;
using GitOut.Features.Git;
using GitOut.Features.Git.RepositoryList;
using GitOut.Features.Git.Stage;
using GitOut.Features.Git.Storage;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Options;
using GitOut.Features.Themes;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Settings
{
    public sealed class SettingsViewModel : INotifyPropertyChanged, INavigationFallback, IDisposable
    {
        private readonly GeneralSettingsViewModel general;
        private ProcessSettingsViewModel? process;
        private object content;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public SettingsViewModel() { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        public SettingsViewModel(
            ITitleService title,
            ISnackbarService snacks,
            IThemeService themes,
            IGitRepositoryStorage repositories,
            IGitRepositoryFactory gitFactory,
            IOptionsMonitor<GitStageOptions> stageOptions,
            IOptionsWriter<GitStageOptions> storage,
            IProcessTelemetryCollector telemetry
        )
        {
            title.Title = "Settings";

            general = new GeneralSettingsViewModel(
                snacks,
                themes,
                repositories,
                gitFactory,
                stageOptions,
                storage
            );
            content = general;
            MenuItem[] menuItems = new[]
            {
                new MenuItem
                {
                    Command = new CallbackCommand(() => CurrentContent = general),
                    IconResourceKey = "Cog",
                    Name = "Settings",
                },
                new MenuItem
                {
                    Command = new CallbackCommand(() =>
                        CurrentContent = (
                            process ??= new ProcessSettingsViewModel(telemetry, snacks)
                        )
                    ),
                    IconResourceKey = "Git",
                    Name = "Git execution log",
                },
            };
            MenuItems = CollectionViewSource.GetDefaultView(menuItems);
        }

        public object CurrentContent
        {
            get => content;
            set => SetProperty(ref content, value);
        }
        public ICollectionView MenuItems { get; }

        public string FallbackPageName => typeof(RepositoryListPage).FullName!;
        public object? FallbackOptions => null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Dispose()
        {
            general.Dispose();
            process?.Dispose();
        }

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
    }
}
