using System;

namespace GitOut.Features.Git.Log
{
    public interface IGitRepositoryMonitor
    {
        event EventHandler LogChanged;
        IGitRepositoryNotifier CreateCallback();
    }
}
