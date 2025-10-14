using System;
using System.Collections.Generic;
using System.Text;

namespace GitOut.Features.Git
{
    public class GitHistoryEvent
    {
        private readonly GitCommitId? mergeParent;

        protected GitHistoryEvent(
            GitCommitId hash,
            GitCommitId? parent,
            GitCommitId? mergeParent,
            DateTimeOffset authorDate,
            GitAuthor author,
            string subject,
            string body
        )
        {
            Id = hash;
            ParentId = parent;
            this.mergeParent = mergeParent;
            AuthorDate = authorDate;
            Author = author;
            Subject = subject;
            Body = body;
        }

        public GitCommitId Id { get; }
        public DateTimeOffset AuthorDate { get; }
        public GitAuthor Author { get; }
        public string Subject { get; }
        public string Body { get; }

        public bool IsHead { get; internal set; }

        public GitCommitId? ParentId { get; }
        public GitHistoryEvent? Parent { get; private set; }
        public GitHistoryEvent? MergedParent { get; private set; }

        public IList<GitBranchName> Branches { get; } = new List<GitBranchName>();
        public IList<GitHistoryEvent> Children { get; } = new List<GitHistoryEvent>();

        public static IGitHistoryEventBuilder<GitHistoryEvent> Builder() =>
            new GitHistoryEventBuilder();

        public void ResolveParents(IDictionary<GitCommitId, GitHistoryEvent> commits)
        {
            if (ParentId is not null && commits.TryGetValue(ParentId, out GitHistoryEvent? commit))
            {
                Parent = commit;
                Parent.Children.Add(this);
            }
            if (
                mergeParent is not null
                && commits.TryGetValue(mergeParent, out GitHistoryEvent? mergeCommit)
            )
            {
                MergedParent = mergeCommit;
                MergedParent.Children.Add(this);
            }
        }

        private class GitHistoryEventBuilder : IGitHistoryEventBuilder<GitHistoryEvent>
        {
            private readonly StringBuilder bodyBuilder = new();

            private GitCommitId? hash;
            private GitCommitId? parent;
            private GitCommitId? mergeParent;
            private DateTimeOffset? authorDate;
            private string? authorName;
            private string? authorEmail;
            private string? subject;

            public GitHistoryEvent Build() =>
                new(
                    hash ?? throw new ArgumentNullException(nameof(hash), "Hash must not be null"),
                    parent,
                    mergeParent,
                    authorDate
                        ?? throw new ArgumentNullException(
                            nameof(authorDate),
                            "Author date must not be null"
                        ),
                    GitAuthor.Create(
                        authorName
                            ?? throw new ArgumentNullException(
                                nameof(authorName),
                                "Author name must not be null"
                            ),
                        authorEmail
                            ?? throw new ArgumentNullException(
                                nameof(authorEmail),
                                "Author email must not be null"
                            )
                    ),
                    subject
                        ?? throw new ArgumentNullException(
                            nameof(subject),
                            "Subject must not be null"
                        ),
                    bodyBuilder.ToString()
                );

            public IGitHistoryEventBuilder<GitHistoryEvent> BuildBody(string body)
            {
                if (body.Length > 0)
                {
                    bodyBuilder.AppendLine(body);
                }
                return this;
            }

            public IGitHistoryEventBuilder<GitHistoryEvent> ParseDate(long unixTime)
            {
                authorDate = DateTimeOffset.FromUnixTimeSeconds(unixTime);
                return this;
            }

            public IGitHistoryEventBuilder<GitHistoryEvent> ParseAuthorEmail(string authorEmail)
            {
                this.authorEmail = authorEmail;
                return this;
            }

            public IGitHistoryEventBuilder<GitHistoryEvent> ParseAuthorName(string authorName)
            {
                this.authorName = authorName;
                return this;
            }

            public IGitHistoryEventBuilder<GitHistoryEvent> ParseHash(string line)
            {
                ReadOnlySpan<char> span = line.AsSpan();
                hash = GitCommitId.FromHash(span[..40]);
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

            public IGitHistoryEventBuilder<GitHistoryEvent> ParseSubject(string subject)
            {
                this.subject = subject;
                return this;
            }
        }
    }
}
