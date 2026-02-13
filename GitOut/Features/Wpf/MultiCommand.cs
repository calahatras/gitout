using System;
using System.Windows.Input;

namespace GitOut.Features.Wpf;

public class MultiCommand : ICommand
{
    private readonly ICommand[] commands;

    public MultiCommand(params ICommand[] commands)
    {
        this.commands = commands;
        foreach (ICommand command in commands)
        {
            command.CanExecuteChanged += OnCanExecuteChanged;
        }
    }

    public bool CanExecute(object? parameter)
    {
        foreach (ICommand command in commands)
        {
            if (!command.CanExecute(parameter))
            {
                return false;
            }
        }
        return true;
    }

    public void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }
        foreach (ICommand command in commands)
        {
            command.Execute(parameter);
        }
    }

    public event EventHandler? CanExecuteChanged;

    private void OnCanExecuteChanged(object? sender, EventArgs e) =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
