using System.Runtime.InteropServices;
using System.Windows;
using GitOut.Features.Material.Snackbar;

namespace GitOut.Features.GlobalCommands
{
    public class CopyHandler : IGlobalCommandHandler
    {
        private readonly ISnackbarService snack;

        public CopyHandler(ISnackbarService snack)
        {
            this.snack = snack;
            Wpf.Commands.Application.Copy.Add(OnCopy);
        }

        private void OnCopy(string? text)
        {
            if (text is not null)
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
        }
    }
}
