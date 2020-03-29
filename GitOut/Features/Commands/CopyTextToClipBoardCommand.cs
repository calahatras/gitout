using System;
using System.Windows;
using System.Windows.Input;

namespace GitOut.Features.Commands
{
    public class CopyTextToClipBoardCommand<TArg> : ICommand
    {
        private readonly Func<TArg, string> gettext;
        private readonly Func<TArg, bool> canexecute;

        public CopyTextToClipBoardCommand(Func<TArg, string> gettext) : this(gettext, o => true) { }
        public CopyTextToClipBoardCommand(Func<TArg, string> gettext, Func<TArg, bool> canexecute)
        {
            this.gettext = gettext;
            this.canexecute = canexecute;
        }

        public bool CanExecute(object parameter) => canexecute((TArg)parameter);

        public void Execute(object parameter)
        {
            string text = gettext((TArg)parameter);
            if (text != null)
            {
                Clipboard.SetText(text);
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
