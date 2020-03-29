using System;
using System.Windows.Input;

namespace GitOut.Features.Material.Snackbar
{
    public class Snack
    {
        public DateTime DateAddedUtc { get; } = DateTime.UtcNow;

        public int Duration { get; set; } = 3000;
        public string? Message { get; set; }

        public string? ActionText { get; set; }
        public ICommand? ActionCommand { get; set; }

        public Exception? Error { get; set; }
    }
}
