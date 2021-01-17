using GitOut.Features.IO;

namespace GitOut.Features.Git
{
    public interface IGitRepositoryFactory
    {
        IGitRepository Create(DirectoryPath path);
    }
}
