using System;
using System.Threading.Tasks;

namespace GitOut.Features.Material.Snackbar
{
    public class SnackbarService : ISnackbarService
    {
        public event EventHandler<SnackEventArgs>? SnackReceived;

        public Task<SnackAction?> ShowAsync(ISnackBuilder snack)
        {
            var source = new TaskCompletionSource<SnackAction?>();
            SendSnack(snack.Build(action => source.SetResult(action)));
            return source.Task;
        }

        public void ShowError(string message, Exception error, TimeSpan? duration = null)
        {
            ISnackBuilder? builder = Snack.Builder();
            builder.WithMessage(message).WithError(error);
            if (duration.HasValue)
            {
                builder.WithDuration(duration.Value);
            }
            SendSnack(builder.Build());
        }

        public void Show(string message) => SendSnack(Snack.Builder().WithMessage(message).Build());

        public void ShowSuccess(string message) =>
            SendSnack(Snack.Builder().WithMessage(message).Build());

        private void SendSnack(Snack snack) =>
            SnackReceived?.Invoke(this, new SnackEventArgs(snack));
    }
}
