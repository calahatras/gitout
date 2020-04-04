using System;

namespace GitOut.Features.Git
{
    public struct GitCommitId : IEquatable<GitCommitId>
    {
        private GitCommitId(string hash) => Hash = hash;

        public string Hash { get; }

        public static GitCommitId FromHash(ReadOnlySpan<char> hash) => new GitCommitId(hash.ToString());

        public override int GetHashCode() => Hash.GetHashCode();

        public bool Equals(GitCommitId obj) => Hash.Equals(obj.Hash);

        public override bool Equals(object? obj) => obj is GitCommitId other && Equals(other);

        public static bool operator ==(GitCommitId left, GitCommitId right) => left.Equals(right);

        public static bool operator !=(GitCommitId left, GitCommitId right) => !(left == right);
    }
}
