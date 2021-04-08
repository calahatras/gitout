using System;
using System.Collections.Generic;

namespace GitOut.Features.Git.Storage
{
    public interface IGitRepositoryStorage
    {
        IObservable<IEnumerable<IGitRepository>> Repositories { get; }

        void Add(IGitRepository repository);
        void Remove(IGitRepository repository);
    }
}
