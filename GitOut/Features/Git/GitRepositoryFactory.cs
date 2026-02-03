using GitOut.Features.Diagnostics;
using GitOut.Features.Git.Diagnostics;
using GitOut.Features.IO;

namespace GitOut.Features.Git;

public class GitRepositoryFactory : IGitRepositoryFactory
{
    private readonly IProcessFactory<IGitProcess> processFactory;

    public GitRepositoryFactory(IProcessFactory<IGitProcess> processFactory) =>
        this.processFactory = processFactory;

    public IGitRepository Create(DirectoryPath path) =>
        LocalGitRepository.InitializeFromPath(path, processFactory);
}
