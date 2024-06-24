using System.Linq;
using NUnit.Framework;

namespace GitOut.Features.Git.Diff
{
    public class GitDiffResultTest
    {
        [Test]
        public void ParseShouldParseIdDiff()
        {
            string[] diff =
            [
                "diff --git a/015ef887f9c85552a727a19a210ddd644aca41f3 b/d09ce91b690c5555ea2f9895614b6086dea5e2a6",
                "index 015ef88..d09ce91 100644",
                "--- a/015ef887f9c85552a727a19a210ddd644aca41f3",
                "+++ b/d09ce91b690c5555ea2f9895614b6086dea5e2a6",
                "@@ -1,2 +1,2 @@",
                "-<EF><BB><BF>using System;",
                "+using System;",
                " using System.Windows;",
                "@@ -4,3 +4,3 @@ using System.Windows.Input;",
                "            ",
                "-namespace GitOut.Features.Commands",
                "+namespace GitOut.Features.Wpf",
                " {",
                "@@ -10,2 +10,3 @@ namespace GitOut.Features.Commands",
                "         private readonly Func<TArg, bool> canexecute;",
                "+        private readonly TextDataFormat format;",
                "         private readonly Action<string>? onCopied;",
            ];
            IGitDiffBuilder builder = GitDiffResult.Builder();
            foreach (string line in diff)
            {
                builder.Feed(line);
            }
            GitDiffResult result = builder.Build();
            Assert.That(result.Text!.Header, Is.EqualTo("diff --git a/015ef887f9c85552a727a19a210ddd644aca41f3 b/d09ce91b690c5555ea2f9895614b6086dea5e2a6\r\nindex 015ef88..d09ce91 100644\r\n--- a/015ef887f9c85552a727a19a210ddd644aca41f3\r\n+++ b/d09ce91b690c5555ea2f9895614b6086dea5e2a6"));
            Assert.That(result.Text!.Hunks.Count(), Is.EqualTo(3));
        }

        [Test]
        public void ParseShouldParseBinaryDiff()
        {
            string[] diff =
            [
                "diff --git a/GitOut/gitout.ico b/GitOut/gitout.ico",
                "new file mode 100644",
                "index 0000000..fe6742d",
                "Binary files /dev/null and b/GitOut/gitout.ico differ",
            ];
            IGitDiffBuilder builder = GitDiffResult.Builder();
            foreach (string line in diff)
            {
                builder.Feed(line);
            }
            Assert.That(builder.IsBinaryFile, Is.True);
        }

        [Test]
        public void ParseShouldParseEmptyFile()
        {
            string[] diff =
            [
                "diff --git a/a.txt b/a.txt",
                "new file mode 100644",
                "index 0000000..ce01362"
                /* there is no @@ or Binary files here, only the three header lines */
            ];
            IGitDiffBuilder builder = GitDiffResult.Builder();
            foreach (string line in diff)
            {
                builder.Feed(line);
            }
            GitDiffResult result = builder.Build();
            Assert.That(result.Text!.Header, Is.EqualTo(string.Empty));
            Assert.That(result.Text!.Hunks.Count(), Is.EqualTo(0));
        }
    }
}
