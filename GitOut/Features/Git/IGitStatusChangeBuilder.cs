using GitOut.Features.IO;

namespace GitOut.Features.Git
{
    public interface IGitStatusChangeBuilder
    {
        GitStatusChangeType Type { get; }
        IGitStatusChangeBuilder MergedFrom(string path);
        void WorkingDirectory(DirectoryPath workingDirectory);
        GitStatusChange Build();
    }
}
