using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using GitOut.Features.Git.Log;
using GitOut.Features.Git.Stage;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Options;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace GitOut.Features.Git;

public class CherryPickIntegrationTest
{
    [Test]
    public async Task CherryPickFlow_ShouldExecuteSuccessfully()
    {
        // 1. Set up the repository mock
        IGitRepository repository = A.Fake<IGitRepository>();
        A.CallTo(() => repository.Name).Returns("integration-repo");

        // Use a TaskCompletionSource to allow awaiting the async operation
        var tcs = new TaskCompletionSource<bool>();
        A.CallTo(() =>
                repository.CherryPickAsync(
                    A<System.Collections.Generic.IEnumerable<string>>._,
                    null
                )
            )
            .Invokes(() => tcs.SetResult(true));

        // 2. Set up the environment mocks
        INavigationService navigationService = A.Fake<INavigationService>();
        var options = new GitLogPageOptions(repository);
        A.CallTo(() =>
                navigationService.GetOptions<GitLogPageOptions>(typeof(GitLogPage).FullName!)
            )
            .Returns(options);

        ISnackbarService snackbarService = A.Fake<ISnackbarService>();
        IOptionsWriter<GitStageOptions> updateStageOptions = A.Fake<
            IOptionsWriter<GitStageOptions>
        >();
        ITitleService titleService = A.Fake<ITitleService>();

        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watcher = A.Fake<IRepositoryWatcher>();
        A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    A<RepositoryWatcherOptions>._
                )
            )
            .Returns(watcher);

        IOptionsMonitor<GitStageOptions> stagingOptions = A.Fake<
            IOptionsMonitor<GitStageOptions>
        >();
        A.CallTo(() => stagingOptions.CurrentValue).Returns(new GitStageOptions());

        // 3. Initialize the ViewModel
        var viewModel = new GitLogViewModel(
            navigationService,
            titleService,
            watchProvider,
            snackbarService,
            stagingOptions,
            updateStageOptions
        );

        // 4. Set up the selected log entry
        GitHistoryEvent commit = GitHistoryEvent
            .Builder()
            .ParseHash(new string('b', 40))
            .ParseDate(1613333029)
            .ParseAuthorName("Integration User")
            .ParseAuthorEmail("integration@example.com")
            .ParseSubject("integration test commit")
            .Build();

        var treeEvent = new GitTreeEvent(commit);
        viewModel.SelectedLogEntries.Add(treeEvent);

        // 5. Execute the Cherry Pick Command
        viewModel.CherryPickCommand.Execute(null);

        // Wait for the async execution to finish simulated by the TaskCompletionSource
        await tcs.Task;

        // 6. Verify full flow
        A.CallTo(() =>
                repository.CherryPickAsync(
                    A<System.Collections.Generic.IEnumerable<string>>.That.Matches(e =>
                        e.First() == commit.Id.Hash
                    ),
                    null
                )
            )
            .MustHaveHappenedOnceExactly();

        A.CallTo(() => snackbarService.ShowSuccess("Cherry-pick completed successfully"))
            .MustHaveHappenedOnceExactly();
    }
}
