using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace GitOut.Features.Logging;

public class FileLogger : ILogger
{
    private readonly string filePath;

    public FileLogger(string filePath) => this.filePath = filePath;

#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
    public IDisposable BeginScope<TState>(TState state) =>
        throw new InvalidOperationException("FileLogger does not support scoped");
#pragma warning restore CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.

    public bool IsEnabled(LogLevel logLevel) => true;

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    public void Log<TState>(
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception exception,
        Func<TState, Exception, string> formatter
    )
    {
        string content =
            $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {logLevel}\t{eventId.Id}\t{formatter(state, exception)}\r\n";
        if (exception is not null)
        {
            content += $"{exception}\r\n";
        }

        _ = Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.AppendAllText(filePath, content);
    }
}
