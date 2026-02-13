using System;

namespace GitOut.Features.Git;

public class GitFileId : GitObjectId, IEquatable<GitFileId>
{
    private GitFileId(string hash)
        : base(hash) { }

    public override int GetHashCode() => base.GetHashCode();

    public bool Equals(GitFileId? obj) =>
        obj is not null && Hash.Equals(obj.Hash, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is GitFileId other && Equals(other);

    public static GitFileId FromHash(ReadOnlySpan<char> hash) => new(hash.ToString());
}
