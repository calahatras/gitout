using System;

namespace GitOut.Features.IO
{
    [Flags]
    public enum RepositoryWatcherOptions
    {
        None = 0,
        Workspace = 1,
        GitFolder = 2,
        All = 3,
    }
}
