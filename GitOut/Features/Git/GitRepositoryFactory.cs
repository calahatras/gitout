using GitOut.Features.Git.Diagnostics;
using GitOut.Features.IO;

namespace GitOut.Features.Git
{
    public class GitRepositoryFactory : IGitRepositoryFactory
    {
        private readonly IGitProcessFactory processFactory;

        public GitRepositoryFactory(
            IGitProcessFactory processFactory
        ) => this.processFactory = processFactory;

        public IGitRepository Create(DirectoryPath path) => LocalGitRepository.InitializeFromPath(path, processFactory);
    }
}
