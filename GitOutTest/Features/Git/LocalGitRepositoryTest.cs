using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitOut.Features.Diagnostics;
using GitOut.Features.Git.Diagnostics;
using GitOut.Features.Git.Diff;
using GitOut.Features.IO;
using Moq;
using NUnit.Framework;

namespace GitOut.Features.Git
{
    public class LocalGitRepositoryTest
    {
        [Test]
        public async Task ExecuteListDiffChangesAsyncShouldCreateCorrectCommand()
        {
            var path = DirectoryPath.Create("c:\\path\\to\\repo");

            var processFactory = new Mock<IProcessFactory<IGitProcess>>();
            var process = new Mock<IGitProcess>();
            string[] output = Array.Empty<string>();
            process.Setup(m => m.ReadLinesAsync(It.IsAny<CancellationToken>())).Returns(output.ToAsyncEnumerable());
            processFactory.Setup(m => m.Create(path, It.IsAny<ProcessOptions>())).Returns(process.Object);

            var actor = LocalGitRepository.InitializeFromPath(path, processFactory.Object);

            GitObjectId change1 = GitCommitId.FromHash(new string('b', 40));
            GitObjectId change2 = GitCommitId.FromHash(new string('c', 40));

            IAsyncEnumerable<GitDiffFileEntry> result = actor.ExecuteListDiffChangesAsync(change1, change2, null);
            await foreach (GitDiffFileEntry item in result) { }

            processFactory.Verify(m => m.Create(path, It.Is<ProcessOptions>(m => m.Arguments == "--no-optional-locks diff-tree --no-color -z cccccccccccccccccccccccccccccccccccccccc bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")), Times.Once);
        }
    }
}
