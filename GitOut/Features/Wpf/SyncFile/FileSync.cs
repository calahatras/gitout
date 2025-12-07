using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace GitOut.Features.Wpf.SyncFile;

public static partial class SyncFileBehavior
{
    public class FileSync : IDisposable
    {
        private readonly Action<string> onUpdate;
        private readonly string directory;
        private readonly string filename;
        private readonly string path;
        private readonly Timer timer;

        private FileSystemWatcher? watcher;
        private string current;

        public FileSync(string path, Action<string> onUpdate)
        {
            this.path = path;
            this.onUpdate = onUpdate;
            if (File.Exists(path))
            {
                current = File.ReadAllText(path);
                onUpdate(current);
            }
            directory = Path.GetDirectoryName(path) ?? string.Empty;
            filename = Path.GetFileName(path);

            if (Directory.Exists(directory))
            {
                watcher = StartFileSystemWatcher(directory, filename);
            }

            timer = new Timer(TimeSpan.FromMilliseconds(250));
            timer.Elapsed += OnTimerElapsed;

            timer.AutoReset = false;
        }

        public string Current
        {
            get => current;
            set
            {
                current = value;
                timer.Stop();
                timer.Start();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                watcher?.Dispose();
                timer.Dispose();
            }
        }

        private static async Task<(string current, string error)> ReadAllTextAsync(string path)
        {
            const int maxRetries = 5;
            const int delayMilliseconds = 50;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    await Task.Delay(delayMilliseconds);

                    using FileStream stream = File.Open(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read
                    );
                    using var reader = new StreamReader(stream);
                    string content = await reader.ReadToEndAsync();
                    return (content, string.Empty);
                }
                catch (IOException) when (attempt < maxRetries - 1)
                {
                    await Task.Delay(delayMilliseconds);
                }
            }

            return (string.Empty, $"Failed to read the file after {maxRetries} attempts.");
        }

        private FileSystemWatcher StartFileSystemWatcher(string directory, string filename)
        {
            var watcher = new FileSystemWatcher(directory, filename)
            {
                NotifyFilter = NotifyFilters.LastWrite,
            };
            watcher.Changed += OnChanged;
            watcher.EnableRaisingEvents = true;
            return watcher;
        }

        private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            Directory.CreateDirectory(directory);
            if (watcher is null)
            {
                watcher = StartFileSystemWatcher(directory, filename);
            }
            watcher.EnableRaisingEvents = false;
            try
            {
                File.WriteAllText(path, current);
                watcher.EnableRaisingEvents = true;
            }
            catch (IOException)
            {
                await Task.Delay(50);
            }
        }

        private async void OnChanged(object sender, FileSystemEventArgs e)
        {
            (string current, string error) = await ReadAllTextAsync(path);
            if (!string.IsNullOrEmpty(current))
            {
                onUpdate(current);
            }
            else
            {
                // error
            }
        }
    }
}
