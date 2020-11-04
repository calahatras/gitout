using System;
using System.Text.RegularExpressions;

namespace GitOut.Features.Git
{
    public class GitObjectId : IEquatable<GitObjectId>
    {
        private static readonly Regex ValidHash = new Regex("[0-9a-f]{40}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
        private static readonly Regex EmptyHash = new Regex("[0]{40}", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

        protected GitObjectId(string hash)
        {
            if (hash.Length != 40)
            {
                throw new ArgumentException("Hash must be 40 characters", nameof(hash));
            }
            if (!ValidHash.IsMatch(hash))
            {
                throw new ArgumentException("Hash is not a valid object id", nameof(hash));
            }
            Hash = hash;
        }

        public string Hash { get; }
        public bool IsEmpty => EmptyHash.IsMatch(Hash);

        public override int GetHashCode() => Hash.GetHashCode();

        public override string ToString() => Hash;

        public bool Equals(GitObjectId? obj) => !(obj is null) && Hash.Equals(obj.Hash);

        public override bool Equals(object? obj) => obj is GitObjectId other && Equals(other);

        public static bool operator ==(GitObjectId left, GitObjectId right) => left.Equals(right);

        public static bool operator !=(GitObjectId left, GitObjectId right) => !(left == right);
    }
}
