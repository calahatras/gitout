using NUnit.Framework;

namespace GitOut.Features.Git
{
    public class GitBranchNameTest
    {
        [TestCase("")]
        [TestCase(null)]
        [TestCase("w")]
        [TestCase("wip/")]
        [TestCase("/root")]
        public void CreateLocalShouldNotAllowInvalidNames(string? input)
        {
            bool isValid = GitBranchName.IsValid(input);
            Assert.False(isValid);
        }

        [TestCase("wip")]
        [TestCase("wip/test")]
        [TestCase("fix/go-1/test")]
        [TestCase("feature-1")]
        [TestCase("feature.1")]
        [TestCase("feature_something")]
        [TestCase("some-&-thing")]
        [TestCase("some@name")]
        public void CreateLocalShouldAllowValidNames(string input)
        {
            bool isValid = GitBranchName.IsValid(input);
            Assert.True(isValid);
        }
    }
}
