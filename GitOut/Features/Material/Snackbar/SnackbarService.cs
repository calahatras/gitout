using System;
using System.Threading;
using GitOut.Features.Wpf;

namespace GitOut.Features.Material.Snackbar
{
    public class SnackbarService : ISnackbarService
    {
        private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(3);
        public event EventHandler<SnackEventArgs>? SnackReceived;

        public void Show(string message) => SendSnack(new Snack
        {
            Message = message
        });

        public void ShowError(string message, Exception error, TimeSpan? duration = null) => SendSnack(new Snack
        {
            Message = message,
            Duration = duration.GetValueOrDefault(DefaultDuration),
            Error = error
        });

        public void ShowSuccess(string message, TimeSpan? duration = null, string? actionText = null, Action? onAction = null)
        {
            TimeSpan delay = duration.GetValueOrDefault(DefaultDuration);
            var token = new CancellationTokenSource(delay);
            SendSnack(new Snack
            {
                Message = message,
                Duration = delay,
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
