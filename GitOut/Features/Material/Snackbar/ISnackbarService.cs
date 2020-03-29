using System;

namespace GitOut.Features.Material.Snackbar
{
    public interface ISnackbarService
    {
        event EventHandler<SnackEventArgs> SnackReceived;

        void Show(string message);
        void ShowError(string message, Exception error);
        void ShowSuccess(string message, int duration = 3000, string? actionText = null, Action? onAction = null);
    }
}
