using System;
using System.Text.RegularExpressions;

namespace GitOut.Features.Git;

public class GitObjectId : IEquatable<GitObjectId>
{
    private static readonly Regex ValidHash = new(
        "[0-9a-f]{40}",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1)
    );
    private static readonly Regex EmptyHash = new(
        "[0]{40}",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1)
    );

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

#pragma warning disable CA1307 // Specify StringComparison for clarity
    public override int GetHashCode() => Hash.GetHashCode();
#pragma warning restore CA1307 // Specify StringComparison for clarity

    public override string ToString() => Hash;

    public bool Equals(GitObjectId? other) =>
        other is not null && Hash.Equals(other.Hash, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is GitObjectId other && Equals(other);

    public static bool operator ==(GitObjectId left, GitObjectId right) => Equals(left, right);

    public static bool operator !=(GitObjectId left, GitObjectId right) => !(left == right);
}
