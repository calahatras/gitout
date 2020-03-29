using System;
using System.Windows.Input;

namespace GitOut.Features.Commands
{
    public class CallbackCommand : CallbackCommand<object>
    {
        public CallbackCommand(Action execute) : base(o => execute()) { }

        public CallbackCommand(Action execute, Func<bool> canexecute) : base(o => execute(), o => canexecute()) { }
    }

    public class CallbackCommand<TArg> : ICommand
    {
        private readonly Action<TArg> execute;
        private readonly Func<TArg, bool> canexecute;

        public CallbackCommand(Action<TArg> execute) : this(execute, o => true) { }

        public CallbackCommand(Action<TArg> execute, Func<TArg, bool> canexecute)
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

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                execute((TArg)parameter);
            }
        }
    }
}
