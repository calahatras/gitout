using System;
using System.Collections.Generic;
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
        private readonly IOptions<NavigationWindowOptions> windowOptions;
        private readonly IWritableStorage storage;
        private readonly ILogger<NavigationService> logger;
        private readonly Stack<Tuple<ContentControl, string?, IServiceScope>> pageStack = new();
        private readonly IDictionary<string, object> pageOptions = new Dictionary<string, object>();

        private Window? currentWindow;

        public NavigationService(
            IServiceProvider provider,
            IHostApplicationLifetime life,
            ITitleService title,
            IThemeService theme,
            IOptions<NavigationWindowOptions> windowOptions,
            IWritableStorage storage,
            ILogger<NavigationService> logger
        )
        {
            this.provider = provider;
            this.windowOptions = windowOptions;
            this.storage = storage;
            titleService = title;
            this.theme = theme;
            this.logger = logger;
            life.ApplicationStopping.Register(() =>
            {
                if (currentWindow is not null)
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

            Wpf.ApplicationCommands.Navigate.Back.Add(Back, CanGoBack);
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
            if (current.DataContext is IDisposable dispose)
            {
                dispose.Dispose();
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
            object? service;
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
            switch (service = scope.ServiceProvider.GetService(pageType))
            {
                case Window window:
                    {
                        logger.LogInformation(LogEventId.Navigation, "Navigating to page " + pageName);
                        currentWindow = window;
                        NavigationWindowOptions cachedValues = windowOptions.Value;
                        if (cachedValues.Width.HasValue)
                        {
                            currentWindow.Width = cachedValues.Width.Value;
                        }
                        if (cachedValues.Height.HasValue)
                        {
                            currentWindow.Height = cachedValues.Height.Value;
                        }
                        if (cachedValues.Top.HasValue)
                        {
                            currentWindow.Top = cachedValues.Top.Value;
                        }
                        if (cachedValues.Left.HasValue)
                        {
                            currentWindow.Left = cachedValues.Left.Value;
                        }
                        currentWindow.Activated += (sender, args) =>
                        {
                            if (pageStack.Count == 0)
                            {
                                return;
                            }
                            (ContentControl current, string? title, IServiceScope _) = pageStack.Peek();
                            if (current.DataContext is INavigationListener listener)
                            {
                                listener.Navigated(NavigationType.Activated);
                            }
                        };
                        currentWindow.Deactivated += (sender, args) =>
                        {
                            (ContentControl current, string? title, IServiceScope _) = pageStack.Peek();
                            if (current.DataContext is INavigationListener listener)
                            {
                                listener.Navigated(NavigationType.Deactivated);
                            }
                        };
                        currentWindow.Closing += (sender, args) =>
                        {
                            if (currentWindow.WindowState == WindowState.Normal)
                            {
                                storage.Write(NavigationWindowOptions.SectionKey, new
                                {
                                    Width = currentWindow.ActualWidth,
                                    Height = currentWindow.ActualHeight,
                                    currentWindow.Left,
                                    currentWindow.Top
                                });
                            }
                        };
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
                        CompositeCommand.RaiseExecuteChanged();
                        CurrentPage = pageName;
                        if (page.DataContext is INavigationListener listener)
                        {
                            listener.Navigated(NavigationType.Initial);
                        }
                    }
                    break;
                default: throw new ArgumentOutOfRangeException("Invalid navigational type: " + (service is not null ? service.ToString() : pageName));
            }
        }

        public T? GetOptions<T>(string pageName) where T : class => pageOptions.TryGetValue(pageName, out object? options) ? options as T : null;

        private void OnNavigationRequested(ContentControl control) => NavigationRequested?.Invoke(this, new NavigationEventArgs(control));
    }
}
