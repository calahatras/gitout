using System;

namespace GitOut.Features.Wpf;

public interface ITitleService
{
    string? Title { get; set; }

    event EventHandler<TitleChangedEventArgs>? TitleChanged;
}
