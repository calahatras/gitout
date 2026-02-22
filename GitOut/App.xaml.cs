using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GitOut.Features.Git;
using GitOut.Features.Git.Ignore;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.RepositoryList;
using GitOut.Features.Git.Stage;
using GitOut.Features.Git.Storage;
using GitOut.Features.Input;
using GitOut.Features.Logging;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Options;
using GitOut.Features.Settings;
using GitOut.Features.Storage;
using GitOut.Features.Text;
using GitOut.Features.Themes;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GitOut;

public partial class App : Application
{
    private readonly IHost host;

    /// <summary>
    /// The application's DI service provider, available after <see cref="OnStartup"/>.
    /// Exposed for attached behaviors and other static helpers that cannot receive DI
    /// services through constructor injection (e.g. <c>KeyboardShortcutsBehavior</c>).
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        host = new HostBuilder()
            .ConfigureAppConfiguration(config =>
                config.AddJsonFile(SettingsOptions.GetSettingsPath(), true, true)
            )
            .ConfigureServices(ConfigureServices)
            .ConfigureLogging(
                (host, builder) =>
                {
                    string logFile = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".gitout",
                        "error.log"
                    );
                    _ = builder.AddProvider(new FileLoggerProvider(logFile));
                }
            )
            .UseConsoleLifetime()
            .Build();

        ILogger<App> logger = host.Services.GetRequiredService<ILogger<App>>();
        RegisterExceptionHandlers(logger);
        _ = host.Services.GetService<Features.Wpf.Commands.Application>();

        IHostApplicationLifetime life =
            host.Services.GetRequiredService<IHostApplicationLifetime>();
        _ = life.ApplicationStarted.Register(LogLifetimeEvent(logger, "Host started"));
        _ = life.ApplicationStopping.Register(LogLifetimeEvent(logger, "Application stopping"));
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        PresentationTraceSources.Refresh();
        _ = PresentationTraceSources.DataBindingSource.Listeners.Add(new ConsoleTraceListener());
        _ = PresentationTraceSources.DataBindingSource.Listeners.Add(new DebugTraceListener());
        PresentationTraceSources.DataBindingSource.Switch.Level =
            SourceLevels.Warning | SourceLevels.Error;

        ILogger<App> logger = host.Services.GetRequiredService<ILogger<App>>();
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(LogEventId.Application, "Application started");
            logger.LogInformation(
                LogEventId.Application,
                "Commit ID: {CommitId}",
                Features.Git.Properties.GitProperties.CommitId
            );
        }
        base.OnStartup(e);
        Services = host.Services;
        var token = new CancellationToken();
        _ = host.RunAsync(token);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        host.Dispose();
        base.OnExit(e);
    }

    private static void LogUnhandledException(ILogger logger, Exception? exception) =>
        logger.LogError(LogEventId.Unhandled, exception, exception?.Message);

    private void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        _ = services.AddScoped<ISnackbarService, SnackbarService>();
        services.AddNavigationServiceWithStartPage<RepositoryListPage>(context.Configuration);

        _ = services.AddScoped<ITitleService, TitleService>();
        _ = services.AddSingleton<IWritableStorage, FileStorage>();
        _ = services.AddScoped<Features.Wpf.Commands.Application>();
        _ = services.AddScoped<IGitIgnoreService, GitIgnoreService>();

        services.AddSettingsFeature();
        services.AddGitFeature();
        _ = services.AddTextPromptFeature();
        services.AddThemeFeature();

        _ = services.AddOptions();

        _ = services
            .AddOptions<GitStoreOptions>()
            .Bind(context.Configuration.GetSection(GitStoreOptions.SectionKey));
        services
            .AddWritableOptions<GitStageOptions>()
            .Bind(context.Configuration, GitStageOptions.SectionKey);
        services
            .AddWritableOptions<GitLogOptions>()
            .Bind(context.Configuration, GitLogOptions.SectionKey);
        services
            .AddWritableOptions<KeyboardShortcutsOptions>()
            .Bind(context.Configuration, KeyboardShortcutsOptions.SectionKey);
        services
            .AddWritableOptions<GitGeneralOptions>()
            .Bind(context.Configuration, GitGeneralOptions.SectionKey);

        services
            .AddWritableOptions<WorktreeOptions>()
            .Bind(context.Configuration, WorktreeOptions.SectionKey);
        _ = services.AddLogging();

        _ = services.AddHostedService<Bootstrap>();
    }

    private void RegisterExceptionHandlers(ILogger<App> logger)
    {
        DispatcherUnhandledException += (s, e) => LogUnhandledException(logger, e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            LogUnhandledException(logger, e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (s, e) =>
            LogUnhandledException(logger, e.Exception);
    }

    private static Action LogLifetimeEvent(ILogger<App> logger, string message) =>
        () => logger.LogInformation(LogEventId.Application, message);
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
