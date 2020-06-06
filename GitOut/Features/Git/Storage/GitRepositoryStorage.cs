using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using GitOut.Features.IO;
using GitOut.Features.Storage;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Git.Storage
{
    public class GitRepositoryStorage : IGitRepositoryStorage
    {
        private readonly IOptionsMonitor<GitStoreOptions> options;
        private readonly IWritableStorage storage;

        public GitRepositoryStorage(
            IOptionsMonitor<GitStoreOptions> options,
            IWritableStorage storage
        )
        {
            this.options = options;
            this.storage = storage;
            var repositories = new BehaviorSubject<IEnumerable<IGitRepository>>(Convert(options.CurrentValue.Repositories ?? Array.Empty<string>()));
            Repositories = repositories;
            options.OnChange(update => repositories.OnNext(Convert(update.Repositories ?? Array.Empty<string>())));
        }

        public IObservable<IEnumerable<IGitRepository>> Repositories { get; }

        public void Add(IGitRepository repository) => storage.Write(GitStoreOptions.SectionKey, (options.CurrentValue.Repositories ?? Array.Empty<string>())
            .Concat(new[] { repository.WorkingDirectory.Directory })
            .ToArray()
        );

        private IEnumerable<IGitRepository> Convert(ICollection<string> repos) => repos
            .Select(DirectoryPath.Create)
            .Select(LocalGitRepository.InitializeFromPath)
            .ToList();
    }
}
