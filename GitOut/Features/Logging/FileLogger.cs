using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace GitOut.Features.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string filePath;

        public FileLogger(string filePath) => this.filePath = filePath;

        public IDisposable BeginScope<TState>(TState state) => throw new InvalidOperationException("FileLogger does not support scoped");

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string content = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {logLevel}\t{eventId.Id}\t{formatter(state, exception)}\r\n";
            if (exception != null)
            {
                content += $"{exception}\r\n";
            }
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.AppendAllText(filePath, content);
        }
    }
}
