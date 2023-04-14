using System;

namespace GitOut.Features.Navigation
{
    public interface INavigationService
    {
        string? CurrentPage { get; }

        event EventHandler<EventArgs>? Closed;
        event EventHandler<NavigationEventArgs> NavigationRequested;

        T? GetOptions<T>(string pageName) where T : class;
        T? GetDialogResult<T>() where T : class;

        void Close();
        void Close<T>(T? result) where T : class;

        void Navigate(string page, object? options);
        INavigationService NavigateNewWindow(string pageName, object? options, NavigationOverrideOptions? overrideOptions = default);
    }
}
