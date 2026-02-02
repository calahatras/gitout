using System;

namespace GitOut.Features.IO;

public class RepositoryWatcherEventArgs : EventArgs
{
    public RepositoryWatcherEventArgs(string repositoryPath) => RepositoryPath = repositoryPath;

    public string RepositoryPath { get; }
}
