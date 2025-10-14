using System;
using System.Windows.Input;

namespace GitOut.Features.Navigation
{
    public class NavigateLocalCommand<T> : ICommand
    {
        private readonly INavigationService navigation;
        private readonly string pagename;
        private readonly Func<T?, object>? options;
        private readonly Func<T?, bool>? canexecute;

        public NavigateLocalCommand(
            INavigationService navigation,
            string pagename,
            Func<T?, object>? options = null,
            Func<T?, bool>? canexecute = null
        )
        {
            this.navigation = navigation;
            this.pagename = pagename;
            this.options = options;
            this.canexecute = canexecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) =>
            canexecute == null || canexecute((T?)parameter);

        public void Execute(object? parameter)
        {
            object? pageOptions = options == null ? null : options((T?)parameter);
            bool newWindow = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
            if (newWindow)
            {
                navigation.NavigateNewWindow(pagename, pageOptions);
            }
            else
            {
                navigation.Navigate(pagename, pageOptions);
            }
        }
    }
}
