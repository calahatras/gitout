using NUnit.Framework;

namespace GitOut.Features.Git;

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
        Assert.That(isValid, Is.False);
    }

    [TestCase("wip")]
    [TestCase("wip/test")]
    [TestCase("fix/go-1/test")]
    [TestCase("feature-1")]
    [TestCase("feature.1")]
    [TestCase("feature_something")]
    [TestCase("some-&-thing")]
    [TestCase("some@name")]
    [TestCase("wip(test)")]
    public void CreateLocalShouldAllowValidNames(string input)
    {
        bool isValid = GitBranchName.IsValid(input);
        Assert.That(isValid, Is.True);
    }
}
