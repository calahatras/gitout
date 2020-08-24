using NUnit.Framework;

namespace GitOut.Features.Git
{
    public class GitStashTest
    {
        [Test]
        public void ParseShouldParseNameFromStashEntry()
        {
            string commandLine = "stash@{0}: WIP on node-2: c49c652 prep diff";
            IGitStashBuilder builder = GitStash.Parse(commandLine);
            Assert.That(builder.Name, Is.EqualTo("stash@{0}"));
        }

        [Test]
        public void ParseShouldBuild()
        {
            string commandLine = "stash@{0}: WIP on node-2: c49c652 prep diff";
            string parentHash = "c49c652121b9f1a3823dce746d45390a6906496d";

            GitStash stash = GitStash.Parse(commandLine)
                .UseParent(parentHash)
                .Build();

            Assert.That(stash.Name, Is.EqualTo("stash@{0}"));
            Assert.That(stash.StashIndex, Is.EqualTo(0));
            Assert.That(stash.FromNode, Is.EqualTo("WIP on node-2"));
            Assert.That(stash.FromParent, Is.EqualTo("c49c652 prep diff"));
            Assert.That(stash.ParentId.Hash, Is.EqualTo(parentHash));
        }
    }
}
