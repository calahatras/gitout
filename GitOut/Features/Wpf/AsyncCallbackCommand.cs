using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GitOut.Features.Wpf
{
    public class AsyncCallbackCommand : AsyncCallbackCommand<object>
    {
        public AsyncCallbackCommand(Func<Task> execute) : base(o => execute()) { }

        public AsyncCallbackCommand(Func<Task> execute, Func<bool> canexecute) : base(o => execute(), o => canexecute()) { }
    }

    public class AsyncCallbackCommand<TArg> : ICommand
    {
        private readonly Func<TArg, Task> execute;
        private readonly Func<TArg, bool> canexecute;

        public AsyncCallbackCommand(Func<TArg, Task> execute) : this(execute, o => true) { }

        public AsyncCallbackCommand(Func<TArg, Task> execute, Func<TArg, bool> canexecute)
        {
            this.execute = execute;
            this.canexecute = canexecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => canexecute((TArg)parameter);

        public async void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                await execute((TArg)parameter);
            }
        }
    }
}
