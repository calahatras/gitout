using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GitOut.Features.Git;
using GitOut.Features.Git.RepositoryList;
using GitOut.Features.Git.Stage;
using GitOut.Features.Git.Storage;
using GitOut.Features.Logging;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Options;
using GitOut.Features.Settings;
using GitOut.Features.Storage;
using GitOut.Features.Themes;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Configuration;
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
                .ConfigureAppConfiguration(config => config.AddJsonFile(SettingsOptions.GetSettingsPath(), true, true))
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
            _ = host.Services.GetService<Features.Wpf.Commands.Application>();

            IHostApplicationLifetime life = host.Services.GetRequiredService<IHostApplicationLifetime>();
            life.ApplicationStarted.Register(LogLifetimeEvent(logger, "Host started"));
            life.ApplicationStopping.Register(LogLifetimeEvent(logger, "Application stopping"));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
            PresentationTraceSources.DataBindingSource.Listeners.Add(new DebugTraceListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning | SourceLevels.Error;

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
            services.AddScoped<ISnackbarService, SnackbarService>();
            services.AddNavigationServiceWithStartPage<RepositoryListPage>(context.Configuration);

            services.AddScoped<ITitleService, TitleService>();
            services.AddSingleton<IWritableStorage, FileStorage>();
            services.AddScoped<Features.Wpf.Commands.Application>();

            services.AddSettingsFeature();
            services.AddGitFeature();
            services.AddThemeFeature();

            services.AddOptions();

            services.AddOptions<GitStoreOptions>().Bind(context.Configuration.GetSection(GitStoreOptions.SectionKey));
            services.AddWritableOptions<GitStageOptions>().Bind(context.Configuration, GitStageOptions.SectionKey);
            services.AddLogging();

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

    public class DebugTraceListener : TraceListener
    {
        public override void Write(string? message) { }

        public override void WriteLine(string? message)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
    }
}
