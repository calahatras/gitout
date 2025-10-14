using NUnit.Framework;

namespace GitOut.Features.Git
{
    public class GitStatusChangeTest
    {
        [Test]
        public void BuilderCanParseStatusChangeLine()
        {
            string line =
                "1 M. N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e GitOut/Features/Git/Log/GitLogPage.xaml";
            GitStatusChange change = GitStatusChange.Parse(line).Build();

            Assert.That(change.Type, Is.EqualTo(GitStatusChangeType.Ordinary));
            Assert.That(change.IndexStatus, Is.EqualTo(GitModifiedStatusType.Modified));
            Assert.That(change.WorkspaceStatus, Is.EqualTo(GitModifiedStatusType.Unmodified));
            Assert.That(
                change.Path.ToString(),
                Is.EqualTo("GitOut/Features/Git/Log/GitLogPage.xaml")
            );
        }

        [Test]
        public void BuilderCanParseUntrackedFile()
        {
            string line = "? GitOut/Features/Git/Log/GitLogPage.xaml";
            GitStatusChange change = GitStatusChange.Parse(line).Build();

            Assert.That(change.Type, Is.EqualTo(GitStatusChangeType.Untracked));
            Assert.That(
                change.Path.ToString(),
                Is.EqualTo("GitOut/Features/Git/Log/GitLogPage.xaml")
            );
        }

        [Test]
        public void BuilderCanParseMergeConflictUnmerged()
        {
            string line =
                "u UU N... 100644 100644 100644 100644 039e0a7cc1b30b61b9ed15f627fbe1bc3ec356b0 9e8f761788fd8307e582e0402828237176e645e3 26bfbac95f8721b022a70c2cd07ca4bc18edb5ff advanced.txt";
            GitStatusChange change = GitStatusChange.Parse(line).Build();

            Assert.That(change.Type, Is.EqualTo(GitStatusChangeType.Unmerged));
            Assert.That(
                change.WorkspaceStatus,
                Is.EqualTo(GitModifiedStatusType.UpdatedButUnmerged)
            );
            Assert.That(change.IndexStatus, Is.EqualTo(GitModifiedStatusType.UpdatedButUnmerged));
            Assert.That(change.Path.ToString(), Is.EqualTo("advanced.txt"));
        }

        [Test]
        public void BuilderCanParseMergeConflictDeleted()
        {
            string line =
                "u DU N... 100644 000000 100644 100644 db1eb37c1e082b97991496dce075552e08b09daf 0000000000000000000000000000000000000000 d5aa6d9ec26d2c0cc6a0d0a6f10926529dad4882 appc.json";
            GitStatusChange change = GitStatusChange.Parse(line).Build();

            Assert.That(change.Type, Is.EqualTo(GitStatusChangeType.Unmerged));
            Assert.That(
                change.WorkspaceStatus,
                Is.EqualTo(GitModifiedStatusType.UpdatedButUnmerged)
            );
            Assert.That(change.IndexStatus, Is.EqualTo(GitModifiedStatusType.Deleted));
            Assert.That(change.Path.ToString(), Is.EqualTo("appc.json"));
        }

        [Test]
        public void BuilderCanParseRename()
        {
            string line =
                "2 R. N... 100644 100644 100644 658a4a0d647bef74ec62be23a63a50108b25ec30 1d5a516939dc3f6e92409d614828e1d49a5f7842 R99 sub/path/File.Designer.cs";
            GitStatusChange change = GitStatusChange
                .Parse(line)
                .MergedFrom("sub/path/Other.Designer.cs")
                .Build();

            Assert.That(change.Type, Is.EqualTo(GitStatusChangeType.RenamedOrCopied));
            Assert.That(change.WorkspaceStatus, Is.EqualTo(GitModifiedStatusType.Unmodified));
            Assert.That(change.IndexStatus, Is.EqualTo(GitModifiedStatusType.Renamed));
            Assert.That(change.Path.ToString(), Is.EqualTo("sub/path/File.Designer.cs"));
        }
    }
}
