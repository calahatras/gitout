using System;
using System.Windows.Input;

namespace GitOut.Features.Wpf;

public class NotNullCallbackCommand<TArg> : ICommand
{
    private readonly Action<TArg> execute;
    private readonly Func<TArg, bool> canexecute;

    public NotNullCallbackCommand(Action<TArg> execute)
        : this(execute, o => true) { }

    public NotNullCallbackCommand(Action<TArg> execute, Func<TArg, bool> canexecute)
    {
        this.execute = execute;
        this.canexecute = canexecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) =>
        parameter is not null && canexecute((TArg)parameter);

    public void Execute(object? parameter)
    {
        if (parameter is not null && CanExecute(parameter))
        {
            execute((TArg)parameter);
        }
    }
}
