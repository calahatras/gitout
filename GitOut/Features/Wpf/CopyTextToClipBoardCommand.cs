using System;
using System.Windows;
using System.Windows.Input;

namespace GitOut.Features.Wpf
{
    public class CopyTextToClipBoardCommand<TArg> : ICommand
    {
        private readonly Func<TArg, string> gettext;
        private readonly Func<TArg, bool> canexecute;
        private readonly TextDataFormat format;
        private readonly Action<string>? onCopied;

        public CopyTextToClipBoardCommand(Func<TArg, string> gettext) : this(gettext, o => true, null) { }
        public CopyTextToClipBoardCommand(Func<TArg, string> gettext, Func<TArg, bool> canexecute, Action<string>? onCopied)
            : this(gettext, canexecute, TextDataFormat.Text, onCopied) { }
        public CopyTextToClipBoardCommand(Func<TArg, string> gettext, Func<TArg, bool> canexecute, TextDataFormat format)
            : this(gettext, canexecute, format, null) { }
        public CopyTextToClipBoardCommand(Func<TArg, string> gettext, Func<TArg, bool> canexecute, TextDataFormat format, Action<string>? onCopied)
        {
            this.gettext = gettext;
            this.canexecute = canexecute;
            this.format = format;
            this.onCopied = onCopied;
        }

        public bool CanExecute(object parameter) => canexecute((TArg)parameter);

        public void Execute(object parameter)
        {
            string text = gettext((TArg)parameter);
            if (text != null)
            {
                Clipboard.SetText(text, format);
                onCopied?.Invoke(text);
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
