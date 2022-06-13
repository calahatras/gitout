using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using GitOut.Features.Git;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.ReleaseNotes;

namespace GitOut.Features.Wpf.Commands
{
    public class Application
    {
        private readonly IServiceProvider provider;
        private readonly ISnackbarService snack;

        public Application(
            IServiceProvider provider,
            ISnackbarService snack
        )
        {
            this.provider = provider;
            this.snack = snack;

            Open = new CompositeCommand<string>(OnOpen);
            Copy = new CompositeCommand<string>(OnCopy);
            RevealInExplorer = new CompositeCommand<string>(OnRevealInExplorer);
            OpenReleaseNotesWindow = new CompositeCommand<IGitRepository>(OnOpenReleaseNotesWindow);
        }

        public static CompositeCommand<string>? Open { get; private set; }
        public static CompositeCommand<string>? Copy { get; private set; }
        public static CompositeCommand<string>? RevealInExplorer { get; private set; }
        public static CompositeCommand<IGitRepository>? OpenReleaseNotesWindow { get; private set; }

        private void OnOpen(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();
                snack.Show($"started {path}");
            }
            catch (Exception e)
            {
                snack.ShowError(e.Message, e);
            }
        }

        private void OnCopy(string text)
        {
            try
            {
                Clipboard.SetText(text, TextDataFormat.UnicodeText);
                snack.ShowSuccess($"Copied {text} to clipboard");
            }
            catch (COMException comException)
            {
                snack.ShowError(comException.Message, comException);
            }
        }

        private void OnRevealInExplorer(string path)
        {
            try
            {
                Process.Start("explorer.exe", $"/s,{path}").Dispose();
            }
            catch (Exception e)
            {
                snack.ShowError(e.Message, e);
            }
        }

        private void OnOpenReleaseNotesWindow(IGitRepository repository)
        {
            if (provider.GetService(typeof(INavigationService)) is INavigationService navigation)
            {
                navigation.NavigateNewWindow(typeof(ReleaseNotesPage).FullName!, new GitRepositoryOptions(repository));
            }
        }
    }
}
