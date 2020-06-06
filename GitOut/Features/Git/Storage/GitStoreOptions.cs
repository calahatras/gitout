using System.Collections.Generic;

namespace GitOut.Features.Git.Storage
{
    public class GitStoreOptions
    {
        public const string SectionKey = "git";
        public ICollection<string>? Repositories { get; set; }
    }
}
