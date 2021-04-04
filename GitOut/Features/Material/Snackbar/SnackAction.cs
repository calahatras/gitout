using System.Windows.Input;

namespace GitOut.Features.Material.Snackbar
{
    public class SnackAction
    {
        public SnackAction(string actionText) => Text = actionText;

        public string Text { get; }

        public ICommand? Command { get; set; }
    }
}
