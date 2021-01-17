using GitOut.Features.IO;

namespace GitOut.Features.Git.Diagnostics
{
    public interface IGitProcessFactory
    {
        IGitProcess Create(DirectoryPath workingDirectory, GitProcessOptions arguments);
    }
}
