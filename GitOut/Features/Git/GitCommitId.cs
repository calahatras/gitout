using System;

namespace GitOut.Features.Git
{
    public class GitCommitId : GitObjectId, IEquatable<GitCommitId>
    {
        private GitCommitId(string hash) : base(hash) { }

        public override int GetHashCode() => base.GetHashCode();

        public bool Equals(GitCommitId? obj) => obj is not null && Hash.Equals(obj.Hash, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is GitCommitId other && Equals(other);

        public static GitCommitId FromHash(ReadOnlySpan<char> hash) => new(hash.ToString());
    }
}
