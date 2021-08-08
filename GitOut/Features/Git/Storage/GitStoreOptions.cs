using System.Collections.Generic;

namespace GitOut.Features.Git.Storage
{
    public class GitStoreOptions
    {
        public const string SectionKey = "git";
#pragma warning disable CA2227 // Collection properties should be read only
        public ICollection<string>? Repositories { get; init; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}
