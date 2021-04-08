using System;
using System.Diagnostics;
using System.Windows.Input;

namespace GitOut.Features.Navigation
{
    public class NavigateExternalCommand : NavigateExternalCommand<object>
    {
        public NavigateExternalCommand(Uri uri) : base(uri) { }
    }

    public class NavigateExternalCommand<TArg> : ICommand
    {
        private Uri? uri;
        private readonly Func<TArg?, Uri>? urilambda;

        protected NavigateExternalCommand(Uri uri) => this.uri = uri;

        public NavigateExternalCommand(Func<TArg?, Uri> urilambda) => this.urilambda = urilambda;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => uri is not null || urilambda!((TArg?)parameter) is not null;

        public void Execute(object? parameter)
        {
            if (uri == null)
            {
                uri = urilambda!((TArg?)parameter);
                if (uri == null)
                {
                    return;
                }
            }
            if (uri.IsAbsoluteUri)
            {
                var info = new ProcessStartInfo(uri.AbsoluteUri)
                {
                    UseShellExecute = true
                };
                using (Process.Start(info)) { }
            }
            else
            {
                throw new NotImplementedException("Relative URI:s are not supported (yet)");
            }
        }
    }
}
