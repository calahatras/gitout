using System.Collections.Generic;

namespace GitOut.Features.Git.Storage
{
    public interface IGitRepositoryStorage
    {
        IEnumerable<IGitRepository> GetAll();
        void Add(IGitRepository repository);
    }
}
