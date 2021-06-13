using System;
using System.Diagnostics;
using GitOut.Features.Material.Snackbar;

namespace GitOut.Features.GlobalCommands
{
    public class OpenHandler : IGlobalCommandHandler
    {
        private readonly ISnackbarService snack;

        public OpenHandler(ISnackbarService snack)
        {
            this.snack = snack;
            Wpf.Commands.Application.Open.Add(OnOpen);
        }

        private void OnOpen(string? path)
        {
            if (path is not null)
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
        }
    }
}
