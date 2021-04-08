using System;

namespace GitOut.Features.Material.Snackbar
{
    public interface ISnackBuilder
    {
        ISnackBuilder AddAction(string text);
        Snack Build();
        Snack Build(Action<SnackAction?> commandHandler);
        ISnackBuilder WithDuration(TimeSpan duration);
        ISnackBuilder WithError(Exception error);
        ISnackBuilder WithMessage(string message);
    }
}
