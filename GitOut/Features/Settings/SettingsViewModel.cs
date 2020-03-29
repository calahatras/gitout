using System;
using System.Windows.Input;
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
            IOptions<SettingsOptions> options
        )
        {
            title.Title = "Inställningar";

            OpenSettingsFolderCommand = new NavigateExternalCommand(new Uri("file://" + options.Value.SettingsFolder));
        }

        public ICommand OpenSettingsFolderCommand { get; }
    }
}
