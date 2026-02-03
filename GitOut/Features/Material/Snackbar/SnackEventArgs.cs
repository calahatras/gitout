using System;

namespace GitOut.Features.Material.Snackbar;

public class SnackEventArgs : EventArgs
{
    public SnackEventArgs(Snack snack) => Snack = snack;

    public Snack Snack { get; set; }
}
