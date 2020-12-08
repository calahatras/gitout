using NUnit.Framework;

namespace GitOut.Features.Git.Diff
{
    public class GitDiffFileEntryTest
    {
        [Test]
        public void ParseShouldParseModifiedGitOutput()
        {
            string input = ":100644 100644 ac63bd40d6b5334e7637bd73cb482e5c531d4de6 60f288a26faabafe355d87d063f5eb16665f8cd2 M";

            GitDiffFileEntry result = GitDiffFileEntry.Parse(input).Build("GitOut/Features/Git/Files/GitDirectoryViewModel.cs");

            Assert.That(result.SourceId, Is.EqualTo(GitFileId.FromHash("ac63bd40d6b5334e7637bd73cb482e5c531d4de6")));
            Assert.That(result.DestinationId, Is.EqualTo(GitFileId.FromHash("60f288a26faabafe355d87d063f5eb16665f8cd2")));
            Assert.That(result.Type, Is.EqualTo(GitDiffType.InPlaceEdit));
            Assert.That(result.SourceFileName.ToString(), Is.EqualTo("GitOut/Features/Git/Files/GitDirectoryViewModel.cs"));
        }

        [Test]
        public void ParseShouldParseCopyEditGitOutput()
        {
            string input = ":100644 100644 cb003f7d55054dda457f7eb8c2a1c9295ed04a51 cb003f7d55054dda457f7eb8c2a1c9295ed04a51 C100";

            GitDiffFileEntry result = GitDiffFileEntry.Parse(input).Build("orig.txt", "something.txt");

            Assert.That(result.SourceId, Is.EqualTo(GitFileId.FromHash("cb003f7d55054dda457f7eb8c2a1c9295ed04a51")));
            Assert.That(result.DestinationId, Is.EqualTo(GitFileId.FromHash("cb003f7d55054dda457f7eb8c2a1c9295ed04a51")));
            Assert.That(result.Type, Is.EqualTo(GitDiffType.CopyEdit));
            Assert.That(result.SourceFileName.ToString(), Is.EqualTo("orig.txt"));
            Assert.That(result.DestinationFileName.ToString(), Is.EqualTo("something.txt"));
        }

        [Test]
        public void ParseShouldParseRenameEditGitOutput()
        {
            string input = ":100644 100644 cb003f7d55054dda457f7eb8c2a1c9295ed04a51 cb003f7d55054dda457f7eb8c2a1c9295ed04a51 R100";

            GitDiffFileEntry result = GitDiffFileEntry.Parse(input).Build("orig.txt", "something.txt");

            Assert.That(result.SourceId, Is.EqualTo(GitFileId.FromHash("cb003f7d55054dda457f7eb8c2a1c9295ed04a51")));
            Assert.That(result.DestinationId, Is.EqualTo(GitFileId.FromHash("cb003f7d55054dda457f7eb8c2a1c9295ed04a51")));
            Assert.That(result.Type, Is.EqualTo(GitDiffType.RenameEdit));
            Assert.That(result.SourceFileName.ToString(), Is.EqualTo("orig.txt"));
            Assert.That(result.DestinationFileName.ToString(), Is.EqualTo("something.txt"));
        }

        [Test]
        public void ParseShouldParseCreateGitOutput()
        {
            string input = ":000000 100644 0000000000000000000000000000000000000000 cb003f7d55054dda457f7eb8c2a1c9295ed04a51 A";

            GitDiffFileEntry result = GitDiffFileEntry.Parse(input).Build("something.txt");

            Assert.That(result.SourceId, Is.EqualTo(GitFileId.FromHash("0000000000000000000000000000000000000000")));
            Assert.That(result.DestinationId, Is.EqualTo(GitFileId.FromHash("cb003f7d55054dda457f7eb8c2a1c9295ed04a51")));
            Assert.That(result.Type, Is.EqualTo(GitDiffType.Create));
            Assert.That(result.SourceFileName.ToString(), Is.EqualTo("something.txt"));
        }

        [Test]
        public void ParseShouldParseDeleteGitOutput()
        {
            string input = ":100644 000000 cb003f7d55054dda457f7eb8c2a1c9295ed04a51 0000000000000000000000000000000000000000 D";

            GitDiffFileEntry result = GitDiffFileEntry.Parse(input).Build("orig.txt");

            Assert.That(result.SourceId, Is.EqualTo(GitFileId.FromHash("cb003f7d55054dda457f7eb8c2a1c9295ed04a51")));
            Assert.That(result.DestinationId, Is.EqualTo(GitFileId.FromHash("0000000000000000000000000000000000000000")));
            Assert.That(result.Type, Is.EqualTo(GitDiffType.Delete));
            Assert.That(result.SourceFileName.ToString(), Is.EqualTo("orig.txt"));
        }
    }
}
