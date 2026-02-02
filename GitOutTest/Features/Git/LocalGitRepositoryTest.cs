using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using GitOut.Features.Diagnostics;
using GitOut.Features.Git.Diagnostics;
using GitOut.Features.Git.Diff;
using GitOut.Features.IO;
using NUnit.Framework;

namespace GitOut.Features.Git;

public class LocalGitRepositoryTest
{
    [Test]
    public async Task ExecuteListDiffChangesAsyncShouldCreateCorrectCommand()
    {
        var path = DirectoryPath.Create("c:\\path\\to\\repo");

        IProcessFactory<IGitProcess> processFactory = A.Fake<IProcessFactory<IGitProcess>>();
        IGitProcess process = A.Fake<IGitProcess>();
        string[] output = [];
        A.CallTo(() => process.ReadLinesAsync(default)).Returns(output.ToAsyncEnumerable());
        Captured<ProcessOptions> capturedProcessOptions = A.Captured<ProcessOptions>();
        A.CallTo(() => processFactory.Create(path, capturedProcessOptions._)).Returns(process);

        var actor = LocalGitRepository.InitializeFromPath(path, processFactory);

        GitObjectId change1 = GitCommitId.FromHash(new string('b', 40));
        GitObjectId change2 = GitCommitId.FromHash(new string('c', 40));

        IAsyncEnumerable<GitDiffFileEntry> result = actor.ListDiffChangesAsync(
            change1,
            change2,
            null
        );
        await foreach (GitDiffFileEntry item in result) { }

        Assert.That(
            capturedProcessOptions.Values[0].Arguments,
            Is.EqualTo(
                "--no-optional-locks diff-tree --no-color -z cccccccccccccccccccccccccccccccccccccccc bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
            )
        );
        A.CallTo(() => processFactory.Create(path, capturedProcessOptions._))
            .MustHaveHappenedOnceExactly();
    }
}
