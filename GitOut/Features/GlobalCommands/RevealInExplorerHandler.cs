using System;
using System.Diagnostics;
using GitOut.Features.Material.Snackbar;

namespace GitOut.Features.GlobalCommands
{
    public class RevealInExplorerHandler : IGlobalCommandHandler
    {
        private readonly ISnackbarService snack;

        public RevealInExplorerHandler(ISnackbarService snack)
        {
            this.snack = snack;
            Wpf.Commands.Application.RevealInExplorer.Add(OnRevealInExplorer);
        }

        private void OnRevealInExplorer(string? path)
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
    }
}
