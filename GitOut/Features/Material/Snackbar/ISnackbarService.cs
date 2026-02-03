using System;
using System.Threading.Tasks;

namespace GitOut.Features.Material.Snackbar;

public interface ISnackbarService
{
    event EventHandler<SnackEventArgs> SnackReceived;

    Task<SnackAction?> ShowAsync(ISnackBuilder snack);
    void Show(string message);
    void ShowError(string message, Exception error, TimeSpan? duration = null);
    void ShowSuccess(string message);
}
