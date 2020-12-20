using System;
using System.Collections.Generic;
using System.Linq;

namespace GitOut.Features.Git.Diff
{
    public class GitDiffHunk
    {
        public const string HunkIdentifier = "@@";

        private GitDiffHunk(IEnumerable<HunkLine> lines) => Lines = lines;

        public IEnumerable<HunkLine> Lines { get; }

        public static GitDiffHunk Parse(IEnumerable<string> lines)
        {
            IList<string> hunk = lines.ToList();
            if (!hunk.Any())
            {
                throw new ArgumentException("Expected lines for hunk but was empty", nameof(hunk));
            }

            string head = hunk.First();
            if (!head.StartsWith($"{HunkIdentifier} "))
            {
                throw new ArgumentException("Lines are not a valid diff hunk, must start and end with @@", nameof(hunk));
            }
            string[] headParts = head.Split(' ');
            string[] fromFileRange = headParts[1].Split(',');
            string[] toFileRange = headParts[2].Split(',');

            int from = int.Parse(fromFileRange[0][1..]);
            int to = int.Parse(toFileRange[0][1..]);
            var headLine = HunkLine.AsHead(head, from, to);

            var hunks = new[] { headLine }
                .Concat(lines.Skip(1).Select(line => line[0] switch
                {
                    '+' => HunkLine.AsAdded(line, to++),
                    '-' => HunkLine.AsRemoved(line, from++),
                    '\\' => HunkLine.AsControl(line, from++, to++),
                    _ => HunkLine.AsLine(line, from++, to++)
                }))
                .ToList();
            return new GitDiffHunk(hunks);
        }
    }
}
