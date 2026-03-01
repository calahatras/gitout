using System.Linq;
using FakeItEasy;
using GitOut.Features.Git.Diff;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace GitOut.Features.Git.Stage;

public class DiffSelectedFilesCommandTest
{
    [Test]
    public void DiffSelectedFilesCommandShouldBeDisabledIfNoFilesSelected()
    {
        (GitStageViewModel actor, ISnackbarService _, IGitRepository _) = CreateViewModel();

        Assert.That(actor.DiffSelectedFilesCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void DiffSelectedFilesCommandShouldBeDisabledIfOneFileSelected()
    {
        (GitStageViewModel actor, ISnackbarService _, IGitRepository _) = CreateViewModel();
        actor.WorkspaceFiles.MoveCurrentToFirst();
        ((StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem!).IsSelected = true;

        Assert.That(actor.DiffSelectedFilesCommand.CanExecute(null), Is.False);
    }

    [Test]
    public void DiffSelectedFilesCommandShouldBeEnabledIfTwoFilesSelected()
    {
        (GitStageViewModel actor, ISnackbarService _, IGitRepository _) = CreateViewModel();
        var files = actor.WorkspaceFiles.Cast<StatusChangeViewModel>().ToList();
        files[0].IsSelected = true;
        files[1].IsSelected = true;

        Assert.That(actor.DiffSelectedFilesCommand.CanExecute(null), Is.True);
    }

    [Test]
    public void DiffSelectedFilesShouldCallRepositoryDiffWithNoIndex()
    {
        (GitStageViewModel actor, ISnackbarService _, IGitRepository repository) =
            CreateViewModel();
        var files = actor.WorkspaceFiles.Cast<StatusChangeViewModel>().ToList();
        files[0].IsSelected = true;
        files[1].IsSelected = true;

        IGitDiffBuilder builder = GitDiffResult.Builder();
        builder.Feed("diff --git a/file1 b/file2");
        A.CallTo(() =>
                repository.DiffAsync(
                    A<RelativeDirectoryPath>._,
                    A<RelativeDirectoryPath>._,
                    A<DiffOptions>._
                )
            )
            .WithAnyArguments()
            .Returns(builder.Build());

        actor.DiffSelectedFilesCommand.Execute(null);

        A.CallTo(() =>
                repository.DiffAsync(
                    A<RelativeDirectoryPath>.That.Matches(p => p.ToString() == "file1.txt"),
                    A<RelativeDirectoryPath>.That.Matches(p => p.ToString() == "file2.txt"),
                    A<DiffOptions>.Ignored
                )
            )
            .MustHaveHappened();
    }

    private static (GitStageViewModel, ISnackbarService, IGitRepository) CreateViewModel()
    {
        IGitRepository repository = A.Fake<IGitRepository>();
        GitStatusChange[] changes =
        [
            GitStatusChange.Parse("? file1.txt").Build(),
            GitStatusChange.Parse("? file2.txt").Build(),
            GitStatusChange.Parse("? file3.txt").Build(),
        ];
        A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(changes));
        A.CallTo(() => repository.WorkingDirectory).Returns(DirectoryPath.Create("C:\\Wrapper"));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        A.CallTo(() => navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!))
            .Returns(stageOptions);

        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    A<RepositoryWatcherOptions>.Ignored
                )
            )
            .Returns(A.Fake<IRepositoryWatcher>());

        ISnackbarService snack = A.Fake<ISnackbarService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        var vm = new GitStageViewModel(navigation, title, watchProvider, snack, options);
        vm.Navigated(NavigationType.Initial);
        return (vm, snack, repository);
    }
}
