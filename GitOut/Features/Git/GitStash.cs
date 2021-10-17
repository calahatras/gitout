using System;
using System.Text;

namespace GitOut.Features.Git
{
    public class GitStash : GitHistoryEvent
    {
        public GitStash(
            GitCommitId id,
            GitCommitId parentId,
            GitCommitId? mergeParent,
            DateTimeOffset authorDate,
            GitAuthor author,
            string subject,
            string body,
            int stashIndex
        ) : base(
            id,
            parentId,
            mergeParent,
            authorDate,
            author,
            subject,
            body
        ) => StashIndex = stashIndex;

        public int StashIndex { get; }

        public static IGitHistoryEventBuilder<GitStash> Builder(int stashIndex) => new GitStashBuilder(stashIndex);

        private class GitStashBuilder : IGitHistoryEventBuilder<GitStash>
        {
            private readonly StringBuilder bodyBuilder = new();

            private readonly int stashIndex;
            private GitCommitId? hash;
            private GitCommitId? parent;
            private GitCommitId? mergeParent;
            private DateTimeOffset? authorDate;
            private string? authorName;
            private string? authorEmail;
            private string? subject;

            public GitStashBuilder(int stashIndex) => this.stashIndex = stashIndex;

            public GitStash Build() => new(
                hash ?? throw new ArgumentNullException(nameof(hash), "Hash must not be null"),
                parent ?? throw new InvalidOperationException("Parent ID must be set when building stash"),
                mergeParent,
                authorDate ?? throw new ArgumentNullException(nameof(authorDate), "Author date must not be null"),
                GitAuthor.Create(
                    authorName ?? throw new ArgumentNullException(nameof(authorName), "Author name must not be null"),
                    authorEmail ?? throw new ArgumentNullException(nameof(authorEmail), "Author email must not be null")
                ),
                subject ?? throw new ArgumentNullException(nameof(subject), "Subject must not be null"),
                bodyBuilder.ToString(),
                stashIndex
            );

            public IGitHistoryEventBuilder<GitStash> BuildBody(string body)
            {
                if (body.Length > 0)
                {
                    bodyBuilder.AppendLine(body);
                }
                return this;
            }

            public IGitHistoryEventBuilder<GitStash> ParseDate(long unixTime)
            {
                authorDate = DateTimeOffset.FromUnixTimeSeconds(unixTime);
                return this;
            }

            public IGitHistoryEventBuilder<GitStash> ParseAuthorEmail(string authorEmail)
            {
                this.authorEmail = authorEmail;
                return this;
            }

            public IGitHistoryEventBuilder<GitStash> ParseAuthorName(string authorName)
            {
                this.authorName = authorName;
                return this;
            }

            public IGitHistoryEventBuilder<GitStash> ParseHash(string line)
            {
                ReadOnlySpan<char> span = line.AsSpan();
                hash = GitCommitId.FromHash(span.Slice(0, 40));
                if (line.Length > 40)
                {
                    parent = GitCommitId.FromHash(span.Slice(40, 40));
                }
                if (line.Length > 80)
                {
                    mergeParent = GitCommitId.FromHash(span.Slice(81, 40));
                }
                return this;
            }

            public IGitHistoryEventBuilder<GitStash> ParseSubject(string subject)
            {
                this.subject = subject;
                return this;
            }
        }
    }
}
