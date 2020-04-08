﻿using System;
namespace GitOut.Features.Navigation
{
    public interface INavigationService
    {
        event EventHandler<NavigationEventArgs> NavigationRequested;
        T? GetOptions<T>(string pageName) where T : class;

        void Navigate(string page, object? options);

        void Back();
        bool CanGoBack();
    }
}