using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using GitOut.Features.Git.Stage;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Options;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace GitOut.Features.Git.Log;

public class GitLogViewModelTest
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    // Note: Fields are initialized in the Setup method, which is called before each test.
    private IGitRepository repository;
    private INavigationService navigationService;
    private ISnackbarService snackbarService;
    private IOptionsWriter<GitStageOptions> updateStageOptions;
    private ITitleService titleService;
    private IGitRepositoryWatcherProvider watchProvider;
    private IOptionsMonitor<GitStageOptions> stagingOptions;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    [SetUp]
    [MemberNotNull(
        nameof(repository),
        nameof(navigationService),
        nameof(snackbarService),
        nameof(updateStageOptions),
        nameof(titleService),
        nameof(watchProvider),
        nameof(stagingOptions)
    )]
    public void Setup()
    {
        repository = A.Fake<IGitRepository>();
        A.CallTo(() => repository.Name).Returns("fake-repo");

        navigationService = A.Fake<INavigationService>();
        var options = new GitLogPageOptions(repository);
        A.CallTo(() =>
                navigationService.GetOptions<GitLogPageOptions>(typeof(GitLogPage).FullName!)
            )
            .Returns(options);

        snackbarService = A.Fake<ISnackbarService>();
        updateStageOptions = A.Fake<IOptionsWriter<GitStageOptions>>();
        titleService = A.Fake<ITitleService>();

        watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watcher = A.Fake<IRepositoryWatcher>();
        A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    A<RepositoryWatcherOptions>._
                )
            )
            .Returns(watcher);

        stagingOptions = A.Fake<IOptionsMonitor<GitStageOptions>>();
        A.CallTo(() => stagingOptions.CurrentValue).Returns(new GitStageOptions());
    }

    [Test]
    public async Task CherryPickCommand_ShouldCallRepository_AndShowSuccessSnackbar()
    {
        IOptionsMonitor<GitLogOptions> logOptions = A.Fake<IOptionsMonitor<GitLogOptions>>();

        // Arrange
        var viewModel = new GitLogViewModel(
            navigationService,
            titleService,
            watchProvider,
            snackbarService,
            stagingOptions,
            updateStageOptions,
            logOptions
        );

        GitHistoryEvent commit = GitHistoryEvent
            .Builder()
            .ParseHash(new string('a', 40))
            .ParseDate(1613333029)
            .ParseAuthorName("user")
            .ParseAuthorEmail("user@example.com")
            .ParseSubject("test commit")
            .Build();

        var treeEvent = new GitTreeEvent(commit);
        viewModel.SelectedLogEntries.Add(treeEvent);

        var tcs = new TaskCompletionSource<bool>();
        A.CallTo(() => repository.CherryPickAsync(A<IEnumerable<string>>._, null))
            .Invokes(() => tcs.SetResult(true));

        // Act
        viewModel.CherryPickCommand.Execute(null);

        await tcs.Task;

        // Assert
        A.CallTo(() =>
                repository.CherryPickAsync(
                    A<IEnumerable<string>>.That.Matches(e => e.First() == commit.Id.Hash),
                    null
                )
            )
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => snackbarService.ShowSuccess("Cherry-pick completed successfully"))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task CherryPickCommand_ShouldHandleConflict_AndShowActionSnackbar()
    {
        IOptionsMonitor<GitLogOptions> logOptions = A.Fake<IOptionsMonitor<GitLogOptions>>();
        // Arrange
        var viewModel = new GitLogViewModel(
            navigationService,
            titleService,
            watchProvider,
            snackbarService,
            stagingOptions,
            updateStageOptions,
            logOptions
        );

        GitHistoryEvent commit = GitHistoryEvent
            .Builder()
            .ParseHash(new string('a', 40))
            .ParseDate(1613333029)
            .ParseAuthorName("user")
            .ParseAuthorEmail("user@example.com")
            .ParseSubject("test commit")
            .Build();

        var treeEvent = new GitTreeEvent(commit);
        viewModel.SelectedLogEntries.Add(treeEvent);

        var tcs = new TaskCompletionSource<SnackAction?>();
        A.CallTo(() => repository.CherryPickAsync(A<IEnumerable<string>>._, null))
            .Throws(new InvalidOperationException("Merge conflict in file.txt"));

        A.CallTo(() => snackbarService.ShowAsync(A<ISnackBuilder>._))
            .Invokes(() => tcs.SetResult(null!))
            .Returns(tcs.Task);

        // Act
        viewModel.CherryPickCommand.Execute(null);

        await tcs.Task;

        // Assert
        A.CallTo(() => snackbarService.ShowAsync(A<ISnackBuilder>._))
            .MustHaveHappenedOnceExactly();
    }
}
