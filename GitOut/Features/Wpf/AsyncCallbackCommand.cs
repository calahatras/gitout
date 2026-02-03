using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GitOut.Features.Wpf;

public class AsyncCallbackCommand : AsyncCallbackCommand<object>
{
    public AsyncCallbackCommand(Func<Task> execute)
        : base(_ => execute()) { }

    public AsyncCallbackCommand(Func<Task> execute, Func<bool> canExecute)
        : base(_ => execute(), _ => canExecute()) { }
}

public class AsyncCallbackCommand<TArg> : ICommand
{
    private readonly Func<TArg?, Task> execute;
    private readonly Func<TArg?, bool> canExecute;

    public AsyncCallbackCommand(Func<TArg?, Task> execute)
        : this(execute, o => true) { }

    public AsyncCallbackCommand(Func<TArg?, Task> execute, Func<TArg?, bool> canExecute)
    {
        this.execute = execute;
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute((TArg?)parameter);

    public async void Execute(object? parameter)
    {
        if (CanExecute(parameter))
        {
            await execute((TArg?)parameter);
        }
    }
}
