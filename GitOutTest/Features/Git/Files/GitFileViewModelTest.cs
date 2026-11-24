using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using GitOut.Features.IO;
using NUnit.Framework;

namespace GitOut.Features.Git.Files;

public class GitFileViewModelTest
{
    private static GitFileEntry CreateBlobEntry(string path = "file.txt") =>
        new(
            GitFileId.FromHash("abcb7caf7a6b8368b4ac4da17863bedbce945dab"),
            GitFileType.Blob,
            [],
            RelativeDirectoryPath.Create(path)
        );

    private static IGitRepository CreateRepository()
    {
        IGitRepository repository = A.Fake<IGitRepository>();
        _ = A.CallTo(() => repository.WorkingDirectory)
            .Returns(DirectoryPath.Create("C:/repo"));
        return repository;
    }

    [Test]
    public async Task DiffResultShouldPassCancellationTokenWithTimeoutToGetBlobStreamAsync()
    {
        IGitRepository repository = CreateRepository();
        CancellationToken capturedToken = CancellationToken.None;
        _ = A.CallTo(() => repository.GetBlobStreamAsync(A<GitFileId>._, A<CancellationToken>._))
            .Invokes(
                (GitFileId _, CancellationToken token) => capturedToken = token
            )
            .Returns(Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("hello"))));

        GitFileViewModel actor = GitFileViewModel.Snapshot(
            repository,
            CreateBlobEntry(),
            RelativeDirectoryPath.Root
        );

        _ = actor.DiffResult;
        await WaitUntilAsync(() => !actor.IsLoading);

        Assert.That(capturedToken.CanBeCanceled, Is.True);
        Assert.That(actor.DiffResult, Is.Not.Null);
    }

    [Test]
    public async Task DiffResultShouldCancelWhenLoadTimeoutExpires()
    {
        IGitRepository repository = CreateRepository();
        CancellationToken capturedToken = CancellationToken.None;
        _ = A.CallTo(() => repository.GetBlobStreamAsync(A<GitFileId>._, A<CancellationToken>._))
            .ReturnsLazily(async call =>
            {
                capturedToken = call.GetArgument<CancellationToken>(1);
                await Task.Delay(TimeSpan.FromSeconds(10), capturedToken);
                return new MemoryStream();
            });

        GitFileViewModel actor = GitFileViewModel.Snapshot(
            repository,
            CreateBlobEntry(),
            RelativeDirectoryPath.Root,
            loadTimeout: TimeSpan.FromMilliseconds(50)
        );

        _ = actor.DiffResult;
        await WaitUntilAsync(() => !actor.IsLoading);

        Assert.That(capturedToken.IsCancellationRequested, Is.True);
        Assert.That(actor.IsLoading, Is.False);
        Assert.That(actor.DiffResult, Is.Null);
    }

    [Test]
    public async Task CancelLoadShouldCancelInProgressGetBlobStreamAsync()
    {
        IGitRepository repository = CreateRepository();
        CancellationToken capturedToken = CancellationToken.None;
        var started = new TaskCompletionSource();
        _ = A.CallTo(() => repository.GetBlobStreamAsync(A<GitFileId>._, A<CancellationToken>._))
            .ReturnsLazily(async call =>
            {
                capturedToken = call.GetArgument<CancellationToken>(1);
                _ = started.TrySetResult();
                await Task.Delay(TimeSpan.FromSeconds(10), capturedToken);
                return new MemoryStream();
            });

        GitFileViewModel actor = GitFileViewModel.Snapshot(
            repository,
            CreateBlobEntry(),
            RelativeDirectoryPath.Root,
            loadTimeout: TimeSpan.FromSeconds(30)
        );

        _ = actor.DiffResult;
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.That(actor.IsLoading, Is.True);

        actor.CancelLoad();
        await WaitUntilAsync(() => !actor.IsLoading);

        Assert.That(capturedToken.IsCancellationRequested, Is.True);
        Assert.That(actor.IsLoading, Is.False);
        Assert.That(actor.DiffResult, Is.Null);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }
    }
}
