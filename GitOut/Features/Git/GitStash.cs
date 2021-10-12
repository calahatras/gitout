using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GitOut.Features.Git
{
    public class GitStash
    {
        private static readonly Regex stashIndexMatcher = new("(?:stash@\\{)(\\d+)(?:\\})", RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));

        public GitStash(
            string name,
            GitCommitId id,
            int stashIndex,
            string fromNode,
            string fromParent,
            GitCommitId parentId
        )
        {
            Name = name;
            Id = id;
            StashIndex = stashIndex;
            FromNode = fromNode;
            FromParent = fromParent;
            ParentId = parentId;
        }

        public string Name { get; }
        public GitCommitId Id { get; }
        public int StashIndex { get; }

        public string FromNode { get; }
        public string FromParent { get; }
        public GitCommitId ParentId { get; }

        public static IGitStashBuilder Parse(string line)
        {
            string[] parts = line.Split(':', 3);
            if (parts.Length != 3)
            {
                throw new ArgumentException("Stash split did not result in 3 parts", nameof(line));
            }
            string name = parts[0];

            Match match = stashIndexMatcher.Match(name);
            if (!match.Success)
            {
                throw new ArgumentException("Stash name is not a valid name, missing index", nameof(line));
            }

            int stashIndex = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            string fromNode = parts[1].TrimStart();
            string fromParent = parts[2].TrimStart();
            return new GitStashBuilder(name, stashIndex, fromNode, fromParent);
        }

        private class GitStashBuilder : IGitStashBuilder
        {
            private readonly string fromNode;
            private readonly int stashIndex;
            private readonly string fromParent;
            private GitCommitId? parentId;
            private GitCommitId? id;

            public GitStashBuilder(string name, int stashIndex, string fromNode, string fromParent)
            {
                Name = name;
                this.stashIndex = stashIndex;
                this.fromNode = fromNode;
                this.fromParent = fromParent;
            }

            public string Name { get; }

            public IGitStashBuilder UseId(GitCommitId hash)
            {
                if (id is null)
                {
                    id = hash;
                }
                else if (parentId is null)
                {
                    parentId = hash;
                }
                else
                {
                    throw new InvalidOperationException("GitStashBuilder can only use one parent id");
                }
                return this;
            }

            public GitStash Build() => new(
                Name,
                id ?? throw new InvalidOperationException("id may not be null when building stash"),
                stashIndex,
                fromNode,
                fromParent,
                parentId ?? throw new InvalidOperationException("Parent id may not be null when building stash")
            );
        }
    }
}
