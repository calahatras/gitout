using System;

namespace GitOut.Features.IO;

public interface IRepositoryWatcher : IDisposable
{
    bool EnableRaisingEvents { get; set; }

    event RepositoryWatcherEventHandler Events;
}
