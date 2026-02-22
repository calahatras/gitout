#pragma warning disable CA1506
using System;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using GitOut.Features.Git.Diff;
using GitOut.Features.Git.Ignore;
using GitOut.Features.Git.Patch;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace GitOut.Features.Git.Stage;

public class GitStageViewModelTest
{
    [Test]
    public void UntrackedItemsShouldBeAddedToWorkspace()
    {
        IGitRepository repository = A.Fake<IGitRepository>();
        GitStatusChange[] changes = [GitStatusChange.Parse("? new.txt").Build()];
        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(changes));
        var stageOptions = new GitStagePageOptions(repository);

        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        actor.Navigated(NavigationType.Initial);

        Assert.That(actor.IndexFiles.IsEmpty, Is.True);
        Assert.That(actor.WorkspaceFiles.IsEmpty, Is.False);
        int workspaceCount = 0;
        foreach (object item in actor.WorkspaceFiles)
        {
            ++workspaceCount;
        }
        Assert.That(workspaceCount, Is.EqualTo(1));
        Assert.That(actor.WorkspaceFiles.MoveCurrentToFirst(), Is.True);
        Assert.That(
            ((StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem).Path,
            Is.EqualTo("new.txt")
        );
    }

    [Test]
    public void UntrackedItemsShouldBeRemovedFromWorkspaceWhenStaged()
    {
        IGitRepository repository = A.Fake<IGitRepository>();
        GitStatusChange[] initial = [GitStatusChange.Parse("? new.txt").Build()];
        GitStatusChange[] staged =
        [
            GitStatusChange
                .Parse(
                    "1 A. N... 000000 100644 100644 0000000000000000000000000000000000000000 abcb7caf7a6b8368b4ac4da17863bedbce945dab new.txt"
                )
                .Build(),
        ];
        A.CallTo(() => repository.StatusAsync())
            .ReturnsNextFromSequence(new GitStatusResult(initial), new GitStatusResult(staged));
        var stageOptions = new GitStagePageOptions(repository);

        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        actor.Navigated(NavigationType.Initial);
        actor.RefreshStatusCommand.Execute(null);

        Assert.That(actor.IndexFiles.IsEmpty, Is.False);
        Assert.That(actor.WorkspaceFiles.IsEmpty, Is.True);
        int indexCount = 0;
        foreach (object item in actor.IndexFiles)
        {
            ++indexCount;
        }
        Assert.That(indexCount, Is.EqualTo(1));
        Assert.That(actor.IndexFiles.MoveCurrentToFirst(), Is.True);
        Assert.That(
            ((StatusChangeViewModel)actor.IndexFiles.CurrentItem).Path,
            Is.EqualTo("new.txt")
        );
    }

    [Test]
    public void RefreshShouldHandleRenamedFileWithWorkspaceChanges()
    {
        IGitRepository repository = A.Fake<IGitRepository>();
        GitStatusChange[] changes =
        [
            GitStatusChange
                .Parse(
                    "2 RM N... 100644 100644 100644 aea670e83087b8015c431146dc9812a04b818a79 aea670e83087b8015c431146dc9812a04b818a79 R100 node3.txt node2.txt"
                )
                .MergedFrom("node2.txt")
                .Build(),
        ];
        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(changes));
        var stageOptions = new GitStagePageOptions(repository);

        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        actor.Navigated(NavigationType.Initial);

        Assert.That(actor.IndexFiles.IsEmpty, Is.False);
        Assert.That(actor.WorkspaceFiles.IsEmpty, Is.False);
        int workspaceCount = 0;
        foreach (object item in actor.WorkspaceFiles)
        {
            ++workspaceCount;
        }
        Assert.That(workspaceCount, Is.EqualTo(1));
    }

    [Test]
    public void RefreshShouldRemoveWorkspaceFileAfterStageRefresh()
    {
        IGitRepository repository = A.Fake<IGitRepository>();
        GitStatusChange[] initial =
        [
            GitStatusChange
                .Parse(
                    "1 .M N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c empty.txt"
                )
                .Build(),
        ];
        GitStatusChange[] staged =
        [
            GitStatusChange
                .Parse(
                    "1 M. N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c ef498f7ce55ad80dfa295825fa2fb45bd55ed97f empty.txt"
                )
                .Build(),
        ];
        A.CallTo(() => repository.StatusAsync())
            .ReturnsNextFromSequence(new GitStatusResult(initial), new GitStatusResult(staged));
        var stageOptions = new GitStagePageOptions(repository);

        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        int workspaceCount = 0;
        foreach (object item in actor.WorkspaceFiles)
        {
            ++workspaceCount;
        }
        Assert.That(workspaceCount, Is.EqualTo(1));

        // force a status refresh by calling initial again
        actor.Navigated(NavigationType.Initial);

        workspaceCount = 0;
        foreach (object item in actor.WorkspaceFiles)
        {
            ++workspaceCount;
        }
        Assert.That(workspaceCount, Is.EqualTo(0));

        Assert.That(actor.IndexFiles.IsEmpty, Is.False);
        Assert.That(actor.WorkspaceFiles.IsEmpty, Is.True);

        int stagedCount = 0;
        foreach (object item in actor.IndexFiles)
        {
            ++stagedCount;
        }
        Assert.That(stagedCount, Is.EqualTo(1));
    }

    [Test]
    public void RefreshShouldRemoveAllFilesIfWorkspaceReset()
    {
        IGitRepository repository = A.Fake<IGitRepository>();
        GitStatusChange[] initial =
        [
            GitStatusChange
                .Parse(
                    "1 .M N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c work.txt"
                )
                .Build(),
            GitStatusChange
                .Parse(
                    "1 M. N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c ef498f7ce55ad80dfa295825fa2fb45bd55ed97f index.txt"
                )
                .Build(),
        ];
        GitStatusChange[] next = [];
        A.CallTo(() => repository.StatusAsync())
            .ReturnsNextFromSequence(new GitStatusResult(initial), new GitStatusResult(next));
        var stageOptions = new GitStagePageOptions(repository);

        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        int workspaceCount = 0;
        foreach (object item in actor.WorkspaceFiles)
        {
            ++workspaceCount;
        }
        Assert.That(workspaceCount, Is.EqualTo(1));
        int stagedCount = 0;
        foreach (object item in actor.WorkspaceFiles)
        {
            ++stagedCount;
        }
        Assert.That(stagedCount, Is.EqualTo(1));

        actor.RefreshStatusCommand.Execute(null);

        Assert.That(actor.IndexFiles.IsEmpty, Is.True);
        Assert.That(actor.WorkspaceFiles.IsEmpty, Is.True);
    }

    [Test]
    public void RefreshShouldRemoveSpecificWorkspaceFileIfWorkspaceReset()
    {
        IGitRepository repository = A.Fake<IGitRepository>();
        GitStatusChange[] initial =
        [
            GitStatusChange
                .Parse(
                    "1 .M N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c work1.txt"
                )
                .Build(),
            GitStatusChange
                .Parse(
                    "1 .M N... 100644 100644 100644 aea670e83087b8015c431146dc9812a04b818a79 aea670e83087b8015c431146dc9812a04b818a79 work2.txt"
                )
                .Build(),
            GitStatusChange
                .Parse(
                    "1 M. N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c ef498f7ce55ad80dfa295825fa2fb45bd55ed97f index.txt"
                )
                .Build(),
        ];
        GitStatusChange[] next =
        [
            GitStatusChange
                .Parse(
                    "1 .M N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c work1.txt"
                )
                .Build(),
            GitStatusChange
                .Parse(
                    "1 M. N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c ef498f7ce55ad80dfa295825fa2fb45bd55ed97f index.txt"
                )
                .Build(),
        ];
        A.CallTo(() => repository.StatusAsync())
            .ReturnsNextFromSequence(new GitStatusResult(initial), new GitStatusResult(next));
        var stageOptions = new GitStagePageOptions(repository);

        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        int workspaceCount = 0;
        foreach (object item in actor.WorkspaceFiles)
        {
            ++workspaceCount;
        }
        Assert.That(workspaceCount, Is.EqualTo(2));
        _ = actor.WorkspaceFiles.MoveCurrentToLast();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;

        actor.RefreshStatusCommand.Execute(null);

        bool hasFile = false;
        foreach (StatusChangeViewModel item in actor.WorkspaceFiles)
        {
            if (item.Path == "work2.txt")
            {
                hasFile = true;
                break;
            }
        }
        Assert.That(actor.SelectedChange, Is.Null);
        Assert.That(hasFile, Is.False);
    }

    [Test]
    public void StageSelectedTextCommandShouldCreatePatchWhenStartHunkSelected()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/filename.txt b/filename.txt");
        _ = builder.Feed("index 0123456..789abcd 100644");
        _ = builder.Feed("--- a/filename.txt");
        _ = builder.Feed("+++ b/filename.txt");
        _ = builder.Feed("@@ -1,4 +1,5 @@");
        _ = builder.Feed(" line0");
        _ = builder.Feed("+line1");
        _ = builder.Feed("+line2");
        _ = builder.Feed(" line3");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddIndex))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddIndex,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    2,
                    4
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.StageSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -1,2 +1,4 @@
 line0
+line1
+line2
 line3
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void StageSelectedTextCommandShouldTrimLines()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/filename.txt b/filename.txt");
        _ = builder.Feed("index 0123456..789abcd 100644");
        _ = builder.Feed("--- a/filename.txt");
        _ = builder.Feed("+++ b/filename.txt");
        _ = builder.Feed("@@ -1,4 +1,5 @@");
        _ = builder.Feed(" line0 ");
        _ = builder.Feed("+line1 ");
        _ = builder.Feed("+    ");
        _ = builder.Feed(" line3 ");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false, TrimLineEndings = true });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddIndex))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddIndex,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    0,
                    4
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.StageSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -1,2 +1,4 @@
 line0 
+line1
+
 line3 
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void StageSelectedTextCommandShouldCreatePatchWhenOnlyAddedLinesSelected()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/filename.txt b/filename.txt");
        _ = builder.Feed("index 0123456..789abcd 100644");
        _ = builder.Feed("--- a/filename.txt");
        _ = builder.Feed("+++ b/filename.txt");
        _ = builder.Feed("@@ -1,4 +1,5 @@");
        _ = builder.Feed(" line0");
        _ = builder.Feed("+line1");
        _ = builder.Feed("+line2");
        _ = builder.Feed(" line3");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddIndex))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddIndex,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    2,
                    3
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.StageSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -1,2 +1,4 @@
 line0
+line1
+line2
 line3
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void StageSelectedTextCommandShouldCreatePatchWhenEndHunkSelected()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/filename.txt b/filename.txt");
        _ = builder.Feed("index 0123456..789abcd 100644");
        _ = builder.Feed("--- a/filename.txt");
        _ = builder.Feed("+++ b/filename.txt");
        _ = builder.Feed("@@ -1,4 +1,5 @@");
        _ = builder.Feed(" line0");
        _ = builder.Feed("+line1");
        _ = builder.Feed("+line2");
        _ = builder.Feed(" line3");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddIndex))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddIndex,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    0,
                    4
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.StageSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -1,2 +1,4 @@
 line0
+line1
+line2
 line3
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void StageSelectedTextCommandShouldCreatePatchWhenFullHunkSelected()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/filename.txt b/filename.txt");
        _ = builder.Feed("index 0123456..789abcd 100644");
        _ = builder.Feed("--- a/filename.txt");
        _ = builder.Feed("+++ b/filename.txt");
        _ = builder.Feed("@@ -1,4 +1,5 @@");
        _ = builder.Feed(" line0");
        _ = builder.Feed("+line1");
        _ = builder.Feed("+line2");
        _ = builder.Feed(" line3");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddIndex))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddIndex,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    0,
                    4
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.StageSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -1,2 +1,4 @@
 line0
+line1
+line2
 line3
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void StageSelectedTextCommandShouldCreatePatchWhenCrossingHunks()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/advanced.txt b/advanced.txt");
        _ = builder.Feed("index 9e8f761..914708a 100644");
        _ = builder.Feed("--- a/advanced.txt");
        _ = builder.Feed("+++ b/advanced.txt");
        _ = builder.Feed("@@ -12,2 +12,3 @@ line1110");
        _ = builder.Feed(" line12");
        _ = builder.Feed("++line1213");
        _ = builder.Feed(" line1312");
        _ = builder.Feed("@@ -19 +20,3 @@ line18");
        _ = builder.Feed(" line1925");
        _ = builder.Feed("+");
        _ = builder.Feed(" line");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e advanced.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddIndex))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddIndex,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    0,
                    7
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.StageSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/advanced.txt b/advanced.txt
--- a/advanced.txt
+++ b/advanced.txt
@@ -12,2 +12,3 @@
 line12
++line1213
 line1312
@@ -19,2 +20,3 @@
 line1925
+
 line
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void StageSelectedTextCommandShouldCreatePatchWhenCrossingHunksAndEndIsOfTypeAdded()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/advanced.txt b/advanced.txt");
        _ = builder.Feed("index 9e8f761..914708a 100644");
        _ = builder.Feed("--- a/advanced.txt");
        _ = builder.Feed("+++ b/advanced.txt");
        _ = builder.Feed("@@ -12,2 +12,3 @@ line1110");
        _ = builder.Feed(" line12");
        _ = builder.Feed("++line1213");
        _ = builder.Feed(" line1312");
        _ = builder.Feed("@@ -19 +20,3 @@ line18");
        _ = builder.Feed(" line1925");
        _ = builder.Feed("+");
        _ = builder.Feed("+line 12121");
        _ = builder.Feed("\\ No newline at end of file");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e advanced.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddIndex))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddIndex,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    0,
                    7
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.StageSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/advanced.txt b/advanced.txt
--- a/advanced.txt
+++ b/advanced.txt
@@ -12,2 +12,3 @@
 line12
++line1213
 line1312
@@ -19,1 +20,3 @@
 line1925
+
+line 12121".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void StageSelectedTextCommandShouldNotTrimRemovedLines()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/filename.txt b/filename.txt");
        _ = builder.Feed("index 0123456..789abcd 100644");
        _ = builder.Feed("--- a/filename.txt");
        _ = builder.Feed("+++ b/filename.txt");
        _ = builder.Feed("@@ -18,3 +18,3 @@");
        _ = builder.Feed(" line1717");
        _ = builder.Feed("-line18  ");
        _ = builder.Feed("+line  19");
        _ = builder.Feed(" line  24");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddIndex))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddIndex,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    2,
                    2
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.StageSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -18,3 +18,2 @@
 line1717
-line18  
 line  24
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void StageSelectedTextCommandShouldCreatePatchWhenAddedAndRemovedLines()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/filename.txt b/filename.txt");
        _ = builder.Feed("index 0123456..789abcd 100644");
        _ = builder.Feed("--- a/filename.txt");
        _ = builder.Feed("+++ b/filename.txt");
        _ = builder.Feed("@@ -18,4 +18,8 @@");
        _ = builder.Feed(" line1717");
        _ = builder.Feed("-line18  ");
        _ = builder.Feed(" line  18");
        _ = builder.Feed("+line  19");
        _ = builder.Feed("+line  20");
        _ = builder.Feed("+line  21");
        _ = builder.Feed("+line  22");
        _ = builder.Feed("+line  23");
        _ = builder.Feed(" line  24");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddIndex))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddIndex,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    5,
                    7
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.StageSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -20,2 +20,5 @@
 line  18
+line  20
+line  21
+line  22
 line  24
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void StageSelectedTextCommandShouldCreatePatchWhenMixedSelection()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/filename.txt b/filename.txt");
        _ = builder.Feed("index 0123456..789abcd 100644");
        _ = builder.Feed("--- a/filename.txt");
        _ = builder.Feed("+++ b/filename.txt");
        _ = builder.Feed("@@ -4,16 +4,22 @@");
        _ = builder.Feed(" line 4 4");
        _ = builder.Feed(" line 5 5");
        _ = builder.Feed(" line 6 6");
        _ = builder.Feed("-line 7  ");
        _ = builder.Feed(" line 8 7");
        _ = builder.Feed(" line 9 8");
        _ = builder.Feed(" line10 9");
        _ = builder.Feed(" line1110");
        _ = builder.Feed("-line12  ");
        _ = builder.Feed("+line  11");
        _ = builder.Feed(" line1312");
        _ = builder.Feed(" line1413");
        _ = builder.Feed("+line  14");
        _ = builder.Feed(" line1515");
        _ = builder.Feed(" line1616");
        _ = builder.Feed(" line1717");
        _ = builder.Feed("-line18  ");
        _ = builder.Feed("+line  18");
        _ = builder.Feed("+line  19");
        _ = builder.Feed("+line  20");
        _ = builder.Feed("+line  21");
        _ = builder.Feed("+line  22");
        _ = builder.Feed("+line  23");
        _ = builder.Feed("+line  24");
        _ = builder.Feed(" line1925");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddIndex))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddIndex,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    17,
                    23
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.StageSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -17,3 +17,8 @@
 line1717
-line18  
+line  18
+line  19
+line  20
+line  21
+line  22
+line  23
 line1925
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void StageSelectedTextCommandShouldRemoveOneSelectedLine()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/filename.txt b/filename.txt");
        _ = builder.Feed("index 0123456..789abcd 100644");
        _ = builder.Feed("--- a/filename.txt");
        _ = builder.Feed("+++ b/filename.txt");
        _ = builder.Feed("@@ -3,8 +3,8 @@");
        _ = builder.Feed("     \"line1\": false,");
        _ = builder.Feed("     \"line2\": true,");
        _ = builder.Feed("     \"line3\": {");
        _ = builder.Feed("-      \"line4\": true,");
        _ = builder.Feed("-      \"line5\": 50");
        _ = builder.Feed("+      \"line4\": false,");
        _ = builder.Feed("+      \"line5\": 0");
        _ = builder.Feed("     }");
        _ = builder.Feed("   }");
        _ = builder.Feed(" }");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddIndex))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddIndex,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    4,
                    4
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.StageSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -5,3 +5,2 @@
     ""line3"": {
-      ""line4"": true,
       ""line5"": 50
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void ResetSelectedTextCommandShouldCreatePatchWhenMixedSelection()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/filename.txt b/filename.txt");
        _ = builder.Feed("index 0123456..789abcd 100644");
        _ = builder.Feed("--- a/filename.txt");
        _ = builder.Feed("+++ b/filename.txt");
        _ = builder.Feed("@@ -10,10 +10,17 @@");
        _ = builder.Feed(" line10 9");
        _ = builder.Feed(" line1110");
        _ = builder.Feed(" line12");
        _ = builder.Feed("++line1213");
        _ = builder.Feed(" line1312");
        _ = builder.Feed(" line1413");
        _ = builder.Feed(" line1515");
        _ = builder.Feed(" line1616");
        _ = builder.Feed(" line1717");
        _ = builder.Feed("-line18  ");
        _ = builder.Feed("+line  18");
        _ = builder.Feed("+line  19");
        _ = builder.Feed("+line  20");
        _ = builder.Feed("+line  21");
        _ = builder.Feed("+line  22");
        _ = builder.Feed("+line  23");
        _ = builder.Feed("+line  24");
        _ = builder.Feed(" line1925");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 M. N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.ResetIndex))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.ResetIndex,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    13,
                    15
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.IndexFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.IndexFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.ResetSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -20,5 +20,2 @@
 line  19
-line  20
-line  21
-line  22
 line  23
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void ResetSelectedTextCommandShouldResetRemovedLinesFromWorkspace()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/filename.txt b/filename.txt");
        _ = builder.Feed("index 0123456..789abcd 100644");
        _ = builder.Feed("--- a/filename.txt");
        _ = builder.Feed("+++ b/filename.txt");
        _ = builder.Feed("@@ -16,8 +16,11 @@ line1413");
        _ = builder.Feed(" line1515");
        _ = builder.Feed(" line1616");
        _ = builder.Feed(" line1717");
        _ = builder.Feed("-line18  ");
        _ = builder.Feed("+line  18");
        _ = builder.Feed("+line  19");
        _ = builder.Feed(" line  20");
        _ = builder.Feed(" line  21");
        _ = builder.Feed(" line  22");
        _ = builder.Feed("+line  23");
        _ = builder.Feed("+line  24");
        _ = builder.Feed(" line1925");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddWorkspace))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddWorkspace,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    4,
                    5
                )
            );
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.ResetWorkspace))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.ResetWorkspace,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    4,
                    4
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.ResetSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -18,2 +18,3 @@
 line1717
+line18  
 line  18
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void ResetSelectedTextCommandShouldResetRemovedAndAddedLines()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/filename.txt b/filename.txt");
        _ = builder.Feed("index 0123456..789abcd 100644");
        _ = builder.Feed("--- a/filename.txt");
        _ = builder.Feed("+++ b/filename.txt");
        _ = builder.Feed("@@ -16,8 +16,11 @@ line1413");
        _ = builder.Feed(" line1515");
        _ = builder.Feed(" line1616");
        _ = builder.Feed(" line1717");
        _ = builder.Feed("-line18  ");
        _ = builder.Feed("+line  18");
        _ = builder.Feed("+line  19");
        _ = builder.Feed(" line  20");
        _ = builder.Feed(" line  21");
        _ = builder.Feed(" line  22");
        _ = builder.Feed("+line  23");
        _ = builder.Feed("+line  24");
        _ = builder.Feed(" line1925");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddWorkspace))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddWorkspace,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    4,
                    5
                )
            );
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.ResetWorkspace))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.ResetWorkspace,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    4,
                    5
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.ResetSelectedTextCommand.Execute(document);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._)).MustHaveHappenedOnceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -18,3 +18,3 @@
 line1717
+line18  
-line  18
 line  19
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }

    [Test]
    public void ResetSelectedTextCommandShouldCreateUndoPatch()
    {
        string? patchText = null;
        IGitRepository repository = A.Fake<IGitRepository>();
        IGitDiffBuilder builder = GitDiffResult.Builder();
        _ = builder.Feed("diff --git a/filename.txt b/filename.txt");
        _ = builder.Feed("index 0123456..789abcd 100644");
        _ = builder.Feed("--- a/filename.txt");
        _ = builder.Feed("+++ b/filename.txt");
        _ = builder.Feed("@@ -16,8 +16,11 @@ line1413");
        _ = builder.Feed(" line1515");
        _ = builder.Feed(" line1616");
        _ = builder.Feed(" line1717");
        _ = builder.Feed("-line18  ");
        _ = builder.Feed("+line  18");
        _ = builder.Feed("+line  19");
        _ = builder.Feed(" line  20");
        _ = builder.Feed(" line  21");
        _ = builder.Feed(" line  22");
        _ = builder.Feed("+line  23");
        _ = builder.Feed("+line  24");
        _ = builder.Feed(" line1925");
        GitDiffResult result = builder.Build();
        Captured<GitPatch> capturedGitPatch = A.Captured<GitPatch>();
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .Invokes((GitPatch patch) => patchText = patch.Writer.ToString())
            .Returns(Task.CompletedTask);

        GitStatusChange change = GitStatusChange
            .Parse(
                "1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt"
            )
            .Build();
        GitStatusChange[] initial = [change];

        _ = A.CallTo(() => repository.StatusAsync()).Returns(new GitStatusResult(initial));

        var stageOptions = new GitStagePageOptions(repository);
        INavigationService navigation = A.Fake<INavigationService>();
        _ = A.CallTo(() =>
                navigation.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName!)
            )
            .Returns(stageOptions);
        ITitleService title = A.Fake<ITitleService>();
        IGitRepositoryWatcherProvider watchProvider = A.Fake<IGitRepositoryWatcherProvider>();
        IRepositoryWatcher watch = A.Fake<IRepositoryWatcher>();
        _ = A.CallTo(() =>
                watchProvider.PrepareWatchRepositoryChanges(
                    repository,
                    RepositoryWatcherOptions.All
                )
            )
            .Returns(watch);
        ISnackbarService snack = A.Fake<ISnackbarService>();
        IGitIgnoreService ignore = A.Fake<IGitIgnoreService>();
        IOptionsMonitor<GitStageOptions> options = A.Fake<IOptionsMonitor<GitStageOptions>>();
        _ = A.CallTo(() => options.CurrentValue)
            .Returns(new GitStageOptions { ShowSpacesAsDots = false });

        IHunkLineVisitorProvider document = A.Fake<IHunkLineVisitorProvider>();
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.AddWorkspace))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.AddWorkspace,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    4,
                    5
                )
            );
        _ = A.CallTo(() => document.GetHunkVisitor(PatchMode.ResetWorkspace))
            .Returns(
                new DiffHunkLineVisitor(
                    PatchMode.ResetWorkspace,
                    result.Text!.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)),
                    4,
                    5
                )
            );

        var actor = new GitStageViewModel(navigation, title, ignore, watchProvider, snack, options);

        // initialize workspace files with initial change
        actor.Navigated(NavigationType.Initial);
        _ = actor.WorkspaceFiles.MoveCurrentToFirst();
        actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
        _ = A.CallTo(() => repository.DiffAsync(A<RelativeDirectoryPath>._, A<DiffOptions>._))
            .Returns(result);

        actor.ResetSelectedTextCommand.Execute(document);
        actor.UndoPatchCommand.Execute(null);
        _ = A.CallTo(() => repository.ApplyAsync(capturedGitPatch._))
            .MustHaveHappenedTwiceExactly();
        Assert.That(
            patchText!.Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase),
            Is.EqualTo(
                @"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -18,3 +18,3 @@
 line1717
-line18  
+line  18
 line  19
".Replace("\r\n", "\n", StringComparison.OrdinalIgnoreCase)
            )
        );
    }
}
