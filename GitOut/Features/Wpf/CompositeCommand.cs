using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GitOut.Features.Wpf;

public class CompositeCommand : ICommand
{
    private readonly List<(Action action, Func<bool> canAction)> actions = new();
    private readonly List<(Func<Task> asyncAction, Func<bool> canAction)> tasks = new();

    public CompositeCommand() { }

    public CompositeCommand(Action action) => Add(action);

    public CompositeCommand(Action action, Func<bool> canAction) => Add(action, canAction);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public void AddAsync(Func<Task> asyncAction, Func<bool> canAction) =>
        tasks.Add((asyncAction, canAction));

    public void Add(Action action) => actions.Add((action, () => true));

    public void Add(Action action, Func<bool> canAction) => actions.Add((action, canAction));

    public bool CanExecute(object? parameter) => actions.Any(a => a.canAction());

    public async void Execute(object? parameter)
    {
        foreach ((Action action, Func<bool> canAction) in actions)
        {
            if (canAction())
            {
                action();
            }
        }
        await ExecuteAsync();
    }

    private async Task ExecuteAsync()
    {
        foreach ((Func<Task> actionAsync, Func<bool> canAction) in tasks)
        {
            if (canAction())
            {
                await actionAsync();
            }
        }
    }

#pragma warning disable CA1030 // Use events where appropriate
    public static void RaiseExecuteChanged() => CommandManager.InvalidateRequerySuggested();
#pragma warning restore CA1030 // Use events where appropriate
}

public class CompositeCommand<T> : ICommand
    where T : class
{
    private readonly List<(Action<T> action, Func<T, bool> canAction)> actions = new();
    private readonly List<(Func<T, Task> actionAsync, Func<T, bool> canAction)> tasks = new();

    public CompositeCommand() { }

    public CompositeCommand(Action<T> action)
        : this() => Add(action);

    public CompositeCommand(Action<T> action, Func<T, bool> canAction)
        : this() => Add(action, canAction);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public void Add(Action<T> action) => actions.Add((action, _ => true));

    public void Add(Action<T> action, Func<T, bool> canAction) => actions.Add((action, canAction));

    public void AddAsync(Func<T, Task> asyncaction, Func<T, bool> canAction) =>
        tasks.Add((asyncaction, canAction));

    public bool CanExecute(object? parameter) =>
        parameter is T t && actions.Any(a => a.canAction(t) || tasks.Any(a => a.canAction(t)));

    public async void Execute(object? parameter)
    {
        if (parameter is not T arg)
        {
            throw new ArgumentException(
                $"Parameter is not of expected type {typeof(T).FullName}",
                nameof(parameter)
            );
        }

        await ExecuteAsync(arg);
    }

    private async Task ExecuteAsync(T parameter)
    {
        foreach ((Action<T> action, Func<T, bool> canAction) in actions)
        {
            if (canAction(parameter))
            {
                action(parameter);
            }
        }

        await Task.WhenAll(
            tasks.Where(t => t.canAction(parameter)).Select(t => t.actionAsync(parameter))
        );
    }

#pragma warning disable CA1030 // Use events where appropriate
    public void RaiseExecuteChanged() => CommandManager.InvalidateRequerySuggested();
#pragma warning restore CA1030 // Use events where appropriate
}
