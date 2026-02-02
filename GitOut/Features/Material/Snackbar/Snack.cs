using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using GitOut.Features.Wpf;

namespace GitOut.Features.Material.Snackbar;

public class Snack
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(3);

    private Snack(
        string message,
        TimeSpan duration,
        Exception? error,
        IEnumerable<SnackAction> actions,
        CancellationToken token
    )
    {
        Message = message;
        Duration = duration;
        var source = CancellationTokenSource.CreateLinkedTokenSource(token);
        Canceled = source.Token;
        CloseSnackCommand = new CallbackCommand(source.Cancel);
        Error = error;
        Actions = actions;
    }

    public DateTime DateAddedUtc { get; } = DateTime.UtcNow;

    public TimeSpan Duration { get; }
    public string Message { get; }
    public ICommand CloseSnackCommand { get; }
    public IEnumerable<SnackAction> Actions { get; }

    public CancellationToken Canceled { get; }
    public Exception? Error { get; }

    public static ISnackBuilder Builder() => new SnackBuilder();

    private class SnackBuilder : ISnackBuilder
    {
        private readonly IList<string> actions = new List<string>();
        private TimeSpan? duration;
        private string? message;
        private Exception? error;
        private CancellationToken cancellationToken;

        public Snack Build()
        {
            if (actions.Count > 0)
            {
                throw new InvalidOperationException(
                    "Must call Build with action handler if actions are available"
                );
            }
            TimeSpan delay = duration ?? DefaultDuration;
            var token = new CancellationTokenSource(delay);
            return new Snack(
                message ?? throw new InvalidOperationException("Snack message cannot be empty"),
                duration ?? DefaultDuration,
                error,
                Array.Empty<SnackAction>(),
                token.Token
            );
        }

        public Snack Build(Action<SnackAction?> commandHandler)
        {
            var token = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var snack = new Snack(
                message ?? throw new InvalidOperationException("Snack message cannot be empty"),
                duration ?? DefaultDuration,
                error,
                actions
                    .Select(actionText =>
                    {
                        var action = new SnackAction(actionText);
                        action.Command = new CallbackCommand(() =>
                        {
                            token.Cancel();
                            commandHandler(action);
                        });
                        return action;
                    })
                    .ToList(),
                token.Token
            );
            TimeSpan delay = duration ?? DefaultDuration;
            _ = Task.Delay(delay, token.Token)
                .ContinueWith(
                    task => commandHandler(null),
                    TaskContinuationOptions.OnlyOnRanToCompletion
                );
            token.CancelAfter(delay);
            return snack;
        }

        public ISnackBuilder WithCancellation(CancellationToken token)
        {
            cancellationToken = token;
            return this;
        }

        public ISnackBuilder WithMessage(string message)
        {
            this.message = message;
            return this;
        }

        public ISnackBuilder WithDuration(TimeSpan duration)
        {
            this.duration = duration;
            return this;
        }

        public ISnackBuilder WithError(Exception error)
        {
            this.error = error;
            return this;
        }

        public ISnackBuilder AddAction(string text)
        {
            actions.Add(text);
            return this;
        }
    }
}
