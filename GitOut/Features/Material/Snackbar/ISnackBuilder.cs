using System;
using System.Threading;

namespace GitOut.Features.Material.Snackbar;

public interface ISnackBuilder
{
    ISnackBuilder AddAction(string text);
    Snack Build();
    Snack Build(Action<SnackAction?> commandHandler);
    ISnackBuilder WithCancellation(CancellationToken token);
    ISnackBuilder WithDuration(TimeSpan duration);
    ISnackBuilder WithError(Exception error);
    ISnackBuilder WithMessage(string message);
}
