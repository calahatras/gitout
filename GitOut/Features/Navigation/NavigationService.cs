using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using GitOut.Features.Logging;
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
        private readonly ILogger<NavigationService> logger;
        private readonly Stack<Tuple<ContentControl, string?, IServiceScope>> pageStack = new Stack<Tuple<ContentControl, string?, IServiceScope>>();
        private readonly IDictionary<string, object> pageOptions = new Dictionary<string, object>();

        private Window? currentPage;

        public NavigationService(
            IServiceProvider provider,
            IHostApplicationLifetime life,
            ITitleService title,
            ILogger<NavigationService> logger
        )
        {
            this.provider = provider;
            titleService = title;
            this.logger = logger;
            life.ApplicationStopping.Register(() =>
            {
                if (currentPage != null)
                {
                    try
                    {
                        currentPage.Dispatcher.Invoke(() =>
                        {
                            if (currentPage.IsActive)
                            {
                                currentPage.Close();
                            }
                        });
                    }
                    catch { }
                }
            });
        }

        public event EventHandler<NavigationEventArgs>? NavigationRequested;

        public void Back()
        {
            if (pageStack.Count <= 1)
            {
                return;
            }
            (ContentControl _, string _, IServiceScope scope) = pageStack.Pop();
            (ContentControl latest, string? title, IServiceScope _) = pageStack.Peek();
            OnNavigationRequested(latest);
            titleService.Title = title;
            logger.LogInformation(LogEventId.Navigation, "Navigating back");

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
                case Window page:
                    {
                        logger.LogInformation(LogEventId.Navigation, "Navigating to page " + pageName);
                        currentPage = page;
                        page.Show();
                    }
                    break;
                case UserControl control:
                    {
                        string? currentTitle = titleService.Title;
                        logger.LogInformation(LogEventId.Navigation, "Navigating to control " + pageName);
                        OnNavigationRequested(control);
                        pageStack.Push(new Tuple<ContentControl, string?, IServiceScope>(control, currentTitle, scope));
                    }
                    break;
                default: throw new ArgumentOutOfRangeException("Invalid navigational type: " + service != null ? service.ToString() : pageName);
            }
        }

        public T? GetOptions<T>(string pageName) where T : class => pageOptions.TryGetValue(pageName, out object? options) ? options as T : null;

        private void OnNavigationRequested(ContentControl control) => NavigationRequested?.Invoke(this, new NavigationEventArgs(control));
    }
}
