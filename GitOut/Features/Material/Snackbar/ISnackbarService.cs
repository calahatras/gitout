using System;

namespace GitOut.Features.Material.Snackbar
{
    public interface ISnackbarService
    {
        event EventHandler<SnackEventArgs> SnackReceived;

        void Show(string message);
        void ShowError(string message, Exception error, TimeSpan? duration = null);
        void ShowSuccess(string message, TimeSpan? duration = null, string? actionText = null, Action? onAction = null);
    }
}
