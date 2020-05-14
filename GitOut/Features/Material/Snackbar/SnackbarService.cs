using System;
using System.Threading;
using GitOut.Features.Commands;

namespace GitOut.Features.Material.Snackbar
{
    public class SnackbarService : ISnackbarService
    {
        public event EventHandler<SnackEventArgs>? SnackReceived;

        public void Show(string message) => SendSnack(new Snack
        {
            Message = message
        });

        public void ShowError(string message, Exception error) => SendSnack(new Snack
        {
            Message = message,
            Error = error
        });

        public void ShowSuccess(string message, int duration = 3000, string? actionText = null, Action? onAction = null)
        {
            var token = new CancellationTokenSource(duration);
            SendSnack(new Snack
            {
                Message = message,
                Duration = duration,
                ActionText = actionText,
                Canceled = token.Token,
                ActionCommand = onAction == null ? null : new CallbackCommand(() =>
                {
                    token.Cancel();
                    onAction();
                })
            });
        }

        private void SendSnack(Snack snack) => SnackReceived?.Invoke(this, new SnackEventArgs(snack));
    }
}
