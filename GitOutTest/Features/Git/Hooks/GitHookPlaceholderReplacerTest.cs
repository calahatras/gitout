using FakeItEasy;
using GitOut.Features.IO;
using NUnit.Framework;

namespace GitOut.Features.Git.Hooks;

public class GitHookPlaceholderReplacerTest
{
    [Test]
    public void Replace_EmptyInput_ReturnsEmptyInput()
    {
        string result = GitHookPlaceholderReplacer.Replace("", A.Fake<IGitRepository>());
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void Replace_ValidPlaceholders_AreReplaced()
    {
        IGitRepository repo = A.Fake<IGitRepository>();
        _ = A.CallTo(() => repo.Name).Returns("test-repo");
        _ = A.CallTo(() => repo.WorkingDirectory).Returns(DirectoryPath.Create("C:\\test\\dir"));

        string input = "echo 'Repo: {RepositoryName}, Path: {RepositoryPath}'";
        string result = GitHookPlaceholderReplacer.Replace(input, repo);

        Assert.That(result, Does.Contain("test-repo"));
        Assert.That(result, Does.Contain("C:\\test\\dir"));
    }
}
