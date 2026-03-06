using NUnit.Framework;

namespace GitOut.Features.Git.Worktree
{
    public class MonikerGeneratorTest
    {
        [Test]
        public void Generate_ShouldReturnLowerCaseString()
        {
            string moniker = MonikerGenerator.Generate();
            Assert.That(moniker, Is.Not.Null.And.Not.Empty);
            Assert.That(moniker, Is.EqualTo(moniker.ToLowerInvariant()));
        }
    }
}
