using NUnit.Framework;

namespace GitOut.Features.Git.Diagnostics
{
    public class GitStatusChangeTest
    {
        [Test]
        public void BuilderCanParseStatusChangeLine()
        {
            string line = "1 M. N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e GitOut/Features/Git/Log/GitLogPage.xaml";
            GitStatusChange change = GitStatusChange.Parse(line).Build();

            Assert.That(change.Type, Is.EqualTo(GitStatusChangeType.Ordinary));
            Assert.That(change.IndexStatus, Is.EqualTo(GitModifiedStatusType.Modified));
            Assert.That(change.WorkspaceStatus, Is.EqualTo(GitModifiedStatusType.Unmodified));
            Assert.That(change.Path.ToString(), Is.EqualTo("GitOut/Features/Git/Log/GitLogPage.xaml"));
        }

        [Test]
        public void BuilderCanParseUntrackedFile()
        {
            string line = "? GitOut/Features/Git/Log/GitLogPage.xaml";
            GitStatusChange change = GitStatusChange.Parse(line).Build();

            Assert.That(change.Type, Is.EqualTo(GitStatusChangeType.Untracked));
            Assert.That(change.Path.ToString(), Is.EqualTo("GitOut/Features/Git/Log/GitLogPage.xaml"));
        }
    }
}
