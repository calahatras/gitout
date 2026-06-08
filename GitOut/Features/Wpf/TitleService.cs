using System;

namespace GitOut.Features.Wpf;

public class TitleService : ITitleService
{
    public string? Title
    {
        get;
        set => SetProperty(ref field, value);
    }

    public event EventHandler<TitleChangedEventArgs>? TitleChanged;

    private void SetProperty(ref string? prop, string? value)
    {
        if (!ReferenceEquals(prop, value))
        {
            prop = value;
            TitleChanged?.Invoke(this, new TitleChangedEventArgs(value));
        }
    }
}
