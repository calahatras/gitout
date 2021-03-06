using System;
using System.Threading;
using System.Windows.Input;

namespace GitOut.Features.Material.Snackbar
{
    public class Snack
    {
        public DateTime DateAddedUtc { get; } = DateTime.UtcNow;

        public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(3);
        public string? Message { get; set; }

        public string? ActionText { get; set; }
        public ICommand? ActionCommand { get; set; }

        public CancellationToken Canceled { get; set; }
        public Exception? Error { get; set; }
    }
}
