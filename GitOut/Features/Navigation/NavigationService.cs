using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using GitOut.Features.Logging;
using GitOut.Features.Themes;
using GitOut.Features.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitOut.Features.Navigation
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider provider;
        private readonly ITitleService titleService;
        private readonly IThemeService theme;
        private readonly ILogger<NavigationService> logger;
        private readonly Stack<Tuple<ContentControl, string?, IServiceScope>> pageStack = new Stack<Tuple<ContentControl, string?, IServiceScope>>();
        private readonly IDictionary<string, object> pageOptions = new Dictionary<string, object>();

        private Window? currentWindow;

        public NavigationService(
            IServiceProvider provider,
            IHostApplicationLifetime life,
            ITitleService title,
            IThemeService theme,
            ILogger<NavigationService> logger
        )
        {
            this.provider = provider;
            titleService = title;
            this.theme = theme;
            this.logger = logger;
            life.ApplicationStopping.Register(() =>
            {
                if (currentWindow != null)
                {
                    try
                    {
                        currentWindow.Dispatcher.Invoke(() =>
                        {
                            if (currentWindow.IsActive)
                            {
                                currentWindow.Close();
                            }
                        });
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                }
            });
        }

        public string? CurrentPage { get; private set; }
        public event EventHandler<NavigationEventArgs>? NavigationRequested;

        public void Back()
        {
            if (pageStack.Count <= 1)
            {
                return;
            }
            (ContentControl current, string _, IServiceScope scope) = pageStack.Pop();
            (ContentControl latest, string? title, IServiceScope _) = pageStack.Peek();
            OnNavigationRequested(latest);
            titleService.Title = title;
            CurrentPage = latest.GetType().FullName;
            logger.LogInformation(LogEventId.Navigation, $"Navigating back to {CurrentPage} ({title})");
            if (current.DataContext is INavigationListener navigatedToContext)
            {
                navigatedToContext.Navigated(NavigationType.NavigatedLeave);
            }
            if (latest.DataContext is INavigationListener revisitedContext)
            {
                revisitedContext.Navigated(NavigationType.NavigatedBack);
            }

            scope.Dispose();
        }

        public bool CanGoBack() => pageStack.Count > 1;

        public void Navigate(string pageName, object? options)
        {
            Type pageType = Type.GetType(pageName) ?? throw new ArgumentNullException(nameof(pageName), "Invalid page name " + pageName);
            IServiceScope scope = provider.CreateScope();
            object service;
            if (options != null)
            {
                if (pageOptions.ContainsKey(pageName))
                {
                    pageOptions[pageName] = options;
                }
                else
                {
                    pageOptions.Add(pageName, options);
                }
            }
            switch (service = scope.ServiceProvider.GetService(pageType))
            {
                case Window window:
                    {
                        logger.LogInformation(LogEventId.Navigation, "Navigating to page " + pageName);
                        currentWindow = window;
                        theme.RegisterResourceProvider(window.Resources);
                        window.Show();
                    }
                    break;
                case UserControl page:
                    {
                        string? currentTitle = titleService.Title;
                        logger.LogInformation(LogEventId.Navigation, "Navigating to control " + pageName);
                        OnNavigationRequested(page);
                        pageStack.Push(new Tuple<ContentControl, string?, IServiceScope>(page, currentTitle, scope));
                        CurrentPage = pageName;
                        if (page.DataContext is INavigationListener listener)
                        {
                            listener.Navigated(NavigationType.Initial);
                        }
                    }
                    break;
                default: throw new ArgumentOutOfRangeException("Invalid navigational type: " + service != null ? service.ToString() : pageName);
            }
        }

        public T? GetOptions<T>(string pageName) where T : class => pageOptions.TryGetValue(pageName, out object? options) ? options as T : null;

        private void OnNavigationRequested(ContentControl control) => NavigationRequested?.Invoke(this, new NavigationEventArgs(control));
    }
}
