using System;

namespace GitOut.Features.Git.Log;

public class GitRepositoryMonitor : IGitRepositoryMonitor
{
    public event EventHandler? LogChanged;

    public IGitRepositoryNotifier CreateCallback() => new Notifier(this);

    private class Notifier : IGitRepositoryNotifier
    {
        private readonly GitRepositoryMonitor monitor;

        public Notifier(GitRepositoryMonitor owner) => monitor = owner;

        public void NotifyLogChanged() => monitor.LogChanged?.Invoke(monitor, new EventArgs());
    }
}
