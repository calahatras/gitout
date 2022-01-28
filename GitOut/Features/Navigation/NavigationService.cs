using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GitOut.Features.Logging;
using GitOut.Features.Storage;
using GitOut.Features.Themes;
using GitOut.Features.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Navigation
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider provider;
        private readonly ITitleService titleService;
        private readonly IThemeService theme;
        private readonly ILogger<NavigationService> logger;

        private readonly Stack<Tuple<ContentControl, string?>> pageStack = new();
        private readonly IDictionary<string, object> pageOptions = new Dictionary<string, object>();

        private NavigatorShell? shell;

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
                foreach (Window window in Application.Current.Windows)
                {
                    try
                    {
                        window.Dispatcher.Invoke(() =>
                        {
                            if (window.IsActive)
                            {
                                window.Close();
                            }
                        });
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch { }
#pragma warning restore CA1031 // Do not catch general exception types
                }
            });

            Wpf.Commands.Navigate.Back.Add(Back, CanGoBack);
        }

        public string? CurrentPage { get; private set; }

        public event EventHandler<CancelEventArgs>? Closing;
        public event EventHandler<NavigationEventArgs>? NavigationRequested;

        public void Back()
        {
            if (pageStack.Count <= 1)
            {
                return;
            }
            (ContentControl current, string _) = pageStack.Pop();
            (ContentControl latest, string? title) = pageStack.Peek();
            OnNavigationRequested(latest);
            titleService.Title = title;
            CurrentPage = latest.GetType().FullName;
            logger.LogInformation(LogEventId.Navigation, "Navigating back to {CurrentPage} ({Title})", CurrentPage, title);
            if (current.DataContext is INavigationListener navigatedToContext)
            {
                navigatedToContext.Navigated(NavigationType.NavigatedLeave);
            }
            if (current.DataContext is IDisposable dispose)
            {
                dispose.Dispose();
            }
            if (latest.DataContext is INavigationListener revisitedContext)
            {
                revisitedContext.Navigated(NavigationType.NavigatedBack);
            }
        }

        public bool CanGoBack() =>
            pageStack.Count > 0
            && Window.GetWindow(pageStack.Peek().Item1) == Application.Current.MainWindow;

        public void Navigate(string pageName, object? options, NavigationOptions? navigation = default)
        {
            if (shell is null)
            {
                shell = CreateShell();
            }
            Type pageType = Type.GetType(pageName) ?? throw new ArgumentNullException(nameof(pageName), "Invalid page name " + pageName);

            if (navigation?.OpenInNewWindow ?? false)
            {
                IServiceScope scope = provider.CreateScope();
                logger.LogInformation(LogEventId.Navigation, "Opening new window");
                INavigationService windowNavigation = scope.ServiceProvider.GetRequiredService<INavigationService>();
                windowNavigation.Navigate(pageName, options);
                windowNavigation.Closing += (s, e) => scope.Dispose();
            }
            else
            {
                if (options is not null)
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
                if (provider.GetService(pageType) is not UserControl page)
                {
                    throw new ArgumentException($"No control provided for page {pageName}", nameof(pageName));
                }
                logger.LogInformation(LogEventId.Navigation, "Navigating to control {PageName}", pageName);

                NavigateToControl(page);
                CurrentPage = pageName;
            }
        }

        private NavigatorShell CreateShell()
        {
            var window = new NavigatorShell(
                provider.GetRequiredService<NavigatorShellViewModel>(),
                provider.GetRequiredService<IWritableStorage>(),
                provider.GetRequiredService<IOptions<NavigationWindowOptions>>()
            );
            window.Activated += (sender, args) =>
            {
                Application.Current.MainWindow = window;
                if (pageStack.Count == 0)
                {
                    return;
                }
                (ContentControl current, string? title) = pageStack.Peek();
                if (current.DataContext is INavigationListener listener)
                {
                    listener.Navigated(NavigationType.Activated);
                }
            };
            window.Deactivated += (sender, args) =>
            {
                (ContentControl current, string? title) = pageStack.Peek();
                if (current.DataContext is INavigationListener listener)
                {
                    listener.Navigated(NavigationType.Deactivated);
                }
            };
            window.Closing += (sender, args) => OnClosing(args);
            theme.RegisterResourceProvider(window.Resources);
            window.Show();
            return window;
        }

        public T? GetOptions<T>(string pageName) where T : class => pageOptions.TryGetValue(pageName, out object? options) ? options as T : null;

        private void NavigateToControl(UserControl page)
        {
            string? currentTitle = titleService.Title;
            OnNavigationRequested(page);
            pageStack.Push(new Tuple<ContentControl, string?>(page, currentTitle));
            CompositeCommand.RaiseExecuteChanged();
            if (page.DataContext is INavigationListener listener)
            {
                listener.Navigated(NavigationType.Initial);
            }
        }

        private void OnClosing(CancelEventArgs args) => Closing?.Invoke(this, args);
        private void OnNavigationRequested(ContentControl control) => NavigationRequested?.Invoke(this, new NavigationEventArgs(control));
    }
}
