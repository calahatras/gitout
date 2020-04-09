using System;
using System.Collections.Generic;
using System.Linq;
using GitOut.Features.IO;
using GitOut.Features.Storage;

namespace GitOut.Features.Git.Storage
{
    public class GitRepositoryStorage : IGitRepositoryStorage
    {
        private const string RepositoriesSectionKey = "repositories";
        private readonly IStorage storage;

        public GitRepositoryStorage(
            IStorage storage
        ) => this.storage = storage;

        public void Add(IGitRepository repository) => storage.Set(RepositoriesSectionKey, GetAll()
            .Select(repo => repo.WorkingDirectory.Directory)
            .Concat(new[] { repository.WorkingDirectory.Directory })
            .ToArray()
        );

        public IEnumerable<IGitRepository> GetAll()
        {
            string[]? repos = storage.Get<string[]>(RepositoriesSectionKey);
            if (repos == null)
            {
                return Array.Empty<IGitRepository>();
            }
            return repos
                .Select(DirectoryPath.Create)
                .Select(LocalGitRepository.InitializeFromPath)
                .ToList();
        }
    }
}
