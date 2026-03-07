using System.IO;
using System.Threading.Tasks;
using FakeItEasy;
using GitOut.Features.IO;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace GitOut.Features.Git.Hooks;

public class GitHookManagerTest
{
    private DirectoryPath? sourceDir;
    private DirectoryPath? targetDir;
    private IGitRepository sourceRepo = null!;
    private IGitRepository targetRepo = null!;

    [SetUp]
    public void SetUp()
    {
        sourceDir = DirectoryPath.Create(
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        );
        _ = Directory.CreateDirectory(Path.Combine(sourceDir.Directory, ".git", "hooks"));
        sourceRepo = A.Fake<IGitRepository>();
        _ = A.CallTo(() => sourceRepo.WorkingDirectory).Returns(sourceDir);
        _ = A.CallTo(() => sourceRepo.Name).Returns("source-repo");

        targetDir = DirectoryPath.Create(
            Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
        );
        _ = Directory.CreateDirectory(Path.Combine(targetDir.Directory, ".git", "hooks"));
        targetRepo = A.Fake<IGitRepository>();
        _ = A.CallTo(() => targetRepo.WorkingDirectory).Returns(targetDir);
        _ = A.CallTo(() => targetRepo.Name).Returns("target-repo");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(sourceDir.Directory))
        {
            Directory.Delete(sourceDir.Directory, true);
        }

        if (Directory.Exists(targetDir.Directory))
        {
            Directory.Delete(targetDir.Directory, true);
        }
    }

    [Test]
    public async Task SaveHookAsync_WritesContentToFile()
    {
        IOptionsMonitor<GitHooksOptions> options = A.Fake<IOptionsMonitor<GitHooksOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitHooksOptions { PreferredShellPath = "pwsh" });

        var manager = new GitHookManager(options);

        var hook = new GitHook(GitHookType.PreCommit, "echo 'hello {RepositoryName}'");
        await manager.SaveHookAsync(sourceRepo, hook);

        string expectedPath = Path.Combine(sourceDir.Directory, ".git", "hooks", "pre-commit");
        Assert.That(File.Exists(expectedPath), Is.True);

        string content = await File.ReadAllTextAsync(expectedPath);
        Assert.That(content, Does.Contain("#!pwsh"));
        Assert.That(content, Does.Contain("echo 'hello source-repo'"));
    }

    [Test]
    public async Task GetHookAsync_RetrievesSavedHook()
    {
        IOptionsMonitor<GitHooksOptions> options = A.Fake<IOptionsMonitor<GitHooksOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitHooksOptions { PreferredShellPath = "pwsh" });

        var manager = new GitHookManager(options);

        var hookToSave = new GitHook(GitHookType.PostMerge, "echo 'done'");
        await manager.SaveHookAsync(sourceRepo, hookToSave);

        GitHook? retrievedHook = await manager.GetHookAsync(sourceRepo, GitHookType.PostMerge);

        Assert.That(retrievedHook, Is.Not.Null);
        Assert.That(retrievedHook!.Type, Is.EqualTo(GitHookType.PostMerge));
        Assert.That(retrievedHook.Content, Does.Contain("echo 'done'"));
    }

    [Test]
    public async Task CopyHookAsync_CopiesToTargetRepository()
    {
        IOptionsMonitor<GitHooksOptions> options = A.Fake<IOptionsMonitor<GitHooksOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitHooksOptions { PreferredShellPath = "pwsh" });

        var manager = new GitHookManager(options);

        var hookToSave = new GitHook(GitHookType.PrePush, "echo 'pushing'");
        await manager.SaveHookAsync(sourceRepo, hookToSave);

        await manager.CopyHookAsync(sourceRepo, targetRepo, GitHookType.PrePush);

        string expectedTargetPath = Path.Combine(targetDir.Directory, ".git", "hooks", "pre-push");
        Assert.That(File.Exists(expectedTargetPath), Is.True);

        string content = await File.ReadAllTextAsync(expectedTargetPath);
        Assert.That(content, Does.Contain("echo 'pushing'"));
    }
}
