using GitOut.Features.IO;

namespace GitOut.Features.Git.Diagnostics
{
    public class GitProcessFactory : IGitProcessFactory
    {
        public IGitProcess Create(DirectoryPath workingDirectory, GitProcessOptions arguments) => new GitProcess(workingDirectory, arguments);
    }
}
