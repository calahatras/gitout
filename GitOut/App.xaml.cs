using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GitOut.Features.Git;
using GitOut.Features.Git.RepositoryList;
using GitOut.Features.Logging;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Settings;
using GitOut.Features.Storage;
using GitOut.Features.Themes;
using GitOut.Features.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitOut
{
    public partial class App : Application
    {
        private readonly IHost host;

        public App()
        {
            host = new HostBuilder()
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging((host, builder) =>
                {
                    string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gitout", "error.log");
                    builder.AddProvider(new FileLoggerProvider(logFile));
                })
                .UseConsoleLifetime()
                .Build();

            ILogger<App> logger = host.Services.GetRequiredService<ILogger<App>>();
            RegisterExceptionHandlers(logger);

            IHostApplicationLifetime life = host.Services.GetRequiredService<IHostApplicationLifetime>();
            life.ApplicationStarted.Register(LogLifetimeEvent(logger, "Host started"));
            life.ApplicationStopping.Register(LogLifetimeEvent(logger, "Application stopping"));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            ILogger<App> logger = host.Services.GetRequiredService<ILogger<App>>();
            logger.LogInformation(LogEventId.Application, "Application started");
            base.OnStartup(e);
            var token = new CancellationToken();
            host.RunAsync(token);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            host.Dispose();
            base.OnExit(e);
        }

        private static void LogUnhandledException(ILogger logger, Exception? exception) => logger.LogError(LogEventId.Unhandled, exception, exception?.Message);

        private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ITitleService, TitleService>();
            services.AddSingleton<IStorage, FileStorage>();

            services.AddSettingsFeature();
            services.AddGitFeature();
            services.AddThemeFeature();

            services.AddOptions();
            services.AddOptions<NavigationRegistrationOptions>().Configure(options =>
            {
                options.StartupWindow = typeof(NavigatorShell).FullName!;
                options.StartupType = typeof(RepositoryListPage).FullName!;
            });
            services.AddOptions<SettingsOptions>().Configure(options =>
                options.SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gitout")
            );
            services.AddLogging();

            services.AddTransient<NavigatorShellViewModel>();
            services.AddScoped<NavigatorShell>();

            services.AddHostedService<Bootstrap>();
        }

        private void RegisterExceptionHandlers(ILogger<App> logger)
        {
            DispatcherUnhandledException += (s, e) => LogUnhandledException(logger, e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogUnhandledException(logger, e.ExceptionObject as Exception);
            TaskScheduler.UnobservedTaskException += (s, e) => LogUnhandledException(logger, e.Exception);
        }

        private static Action LogLifetimeEvent(ILogger<App> logger, string message) => () => logger.LogInformation(LogEventId.Application, message);
    }
}
