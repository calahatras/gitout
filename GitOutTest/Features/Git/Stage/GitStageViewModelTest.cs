using System;
using System.Linq;
using System.Threading.Tasks;
using GitOut.Features.Git.Diff;
using GitOut.Features.Git.Patch;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace GitOut.Features.Git.Stage
{
    public class GitStageViewModelTest
    {
        [Test]
        public void UntrackedItemsShouldBeAddedToWorkspace()
        {
            var repository = new Mock<IGitRepository>();
            GitStatusChange[] changes = new[]
            {
                GitStatusChange.Parse("? new.txt").Build()
            };
            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(changes));
            var stageOptions = new GitStagePageOptions(repository.Object);

            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            actor.Navigated(NavigationType.Initial);

            Assert.That(actor.IndexFiles.IsEmpty, Is.True);
            Assert.That(actor.WorkspaceFiles.IsEmpty, Is.False);
            int workspaceCount = 0;
            foreach (object item in actor.WorkspaceFiles) { ++workspaceCount; }
            Assert.That(workspaceCount, Is.EqualTo(1));
            Assert.That(actor.WorkspaceFiles.MoveCurrentToFirst(), Is.True);
            Assert.That(((StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem).Path, Is.EqualTo("new.txt"));
        }

        [Test]
        public void UntrackedItemsShouldBeRemovedFromWorkspaceWhenStaged()
        {
            var repository = new Mock<IGitRepository>();
            GitStatusChange[] initial = new[]
            {
                GitStatusChange.Parse("? new.txt").Build()
            };
            GitStatusChange[] staged = new[]
            {
                GitStatusChange.Parse("1 A. N... 000000 100644 100644 0000000000000000000000000000000000000000 abcb7caf7a6b8368b4ac4da17863bedbce945dab new.txt").Build()
            };
            repository.SetupSequence(m => m.ExecuteStatusAsync())
                .ReturnsAsync(new GitStatusResult(initial))
                .ReturnsAsync(new GitStatusResult(staged));
            var stageOptions = new GitStagePageOptions(repository.Object);

            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            actor.Navigated(NavigationType.Initial);
            actor.RefreshStatusCommand.Execute(null);

            Assert.That(actor.IndexFiles.IsEmpty, Is.False);
            Assert.That(actor.WorkspaceFiles.IsEmpty, Is.True);
            int indexCount = 0;
            foreach (object item in actor.IndexFiles) { ++indexCount; }
            Assert.That(indexCount, Is.EqualTo(1));
            Assert.That(actor.IndexFiles.MoveCurrentToFirst(), Is.True);
            Assert.That(((StatusChangeViewModel)actor.IndexFiles.CurrentItem).Path, Is.EqualTo("new.txt"));
        }

        [Test]
        public void RefreshShouldHandleRenamedFileWithWorkspaceChanges()
        {
            var repository = new Mock<IGitRepository>();
            GitStatusChange[] changes = new[]
            {
                GitStatusChange.Parse("2 RM N... 100644 100644 100644 aea670e83087b8015c431146dc9812a04b818a79 aea670e83087b8015c431146dc9812a04b818a79 R100 node3.txt node2.txt").MergedFrom("node2.txt").Build()
            };
            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(changes));
            var stageOptions = new GitStagePageOptions(repository.Object);

            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            actor.Navigated(NavigationType.Initial);

            Assert.That(actor.IndexFiles.IsEmpty, Is.False);
            Assert.That(actor.WorkspaceFiles.IsEmpty, Is.False);
            int workspaceCount = 0;
            foreach (object item in actor.WorkspaceFiles) { ++workspaceCount; }
            Assert.That(workspaceCount, Is.EqualTo(1));
        }

        [Test]
        public void RefreshShouldRemoveWorkspaceFileAfterStageRefresh()
        {
            var repository = new Mock<IGitRepository>();
            GitStatusChange[] initial = new[]
            {
                GitStatusChange.Parse("1 .M N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c empty.txt").Build()
            };
            GitStatusChange[] staged = new[]
            {
                GitStatusChange.Parse("1 M. N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c ef498f7ce55ad80dfa295825fa2fb45bd55ed97f empty.txt").Build()
            };
            repository.SetupSequence(m => m.ExecuteStatusAsync())
                .ReturnsAsync(new GitStatusResult(initial))
                .ReturnsAsync(new GitStatusResult(staged));
            var stageOptions = new GitStagePageOptions(repository.Object);

            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            int workspaceCount = 0;
            foreach (object item in actor.WorkspaceFiles) { ++workspaceCount; }
            Assert.That(workspaceCount, Is.EqualTo(1));

            // force a status refresh by calling initial again
            actor.Navigated(NavigationType.Initial);

            workspaceCount = 0;
            foreach (object item in actor.WorkspaceFiles) { ++workspaceCount; }
            Assert.That(workspaceCount, Is.EqualTo(0));

            Assert.That(actor.IndexFiles.IsEmpty, Is.False);
            Assert.That(actor.WorkspaceFiles.IsEmpty, Is.True);

            int stagedCount = 0;
            foreach (object item in actor.IndexFiles) { ++stagedCount; }
            Assert.That(stagedCount, Is.EqualTo(1));
        }

        [Test]
        public void RefreshShouldRemoveAllFilesIfWorkspaceReset()
        {
            var repository = new Mock<IGitRepository>();
            GitStatusChange[] initial = new[]
            {
                GitStatusChange.Parse("1 .M N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c work.txt").Build(),
                GitStatusChange.Parse("1 M. N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c ef498f7ce55ad80dfa295825fa2fb45bd55ed97f index.txt").Build()
            };
            GitStatusChange[] next = Array.Empty<GitStatusChange>();
            repository.SetupSequence(m => m.ExecuteStatusAsync())
                .ReturnsAsync(new GitStatusResult(initial))
                .ReturnsAsync(new GitStatusResult(next));
            var stageOptions = new GitStagePageOptions(repository.Object);

            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            int workspaceCount = 0;
            foreach (object item in actor.WorkspaceFiles) { ++workspaceCount; }
            Assert.That(workspaceCount, Is.EqualTo(1));
            int stagedCount = 0;
            foreach (object item in actor.WorkspaceFiles) { ++stagedCount; }
            Assert.That(stagedCount, Is.EqualTo(1));

            actor.RefreshStatusCommand.Execute(null);

            Assert.That(actor.IndexFiles.IsEmpty, Is.True);
            Assert.That(actor.WorkspaceFiles.IsEmpty, Is.True);
        }

        [Test]
        public void RefreshShouldRemoveSpecificWorkspaceFileIfWorkspaceReset()
        {
            var repository = new Mock<IGitRepository>();
            GitStatusChange[] initial = new[]
            {
                GitStatusChange.Parse("1 .M N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c work1.txt").Build(),
                GitStatusChange.Parse("1 .M N... 100644 100644 100644 aea670e83087b8015c431146dc9812a04b818a79 aea670e83087b8015c431146dc9812a04b818a79 work2.txt").Build(),
                GitStatusChange.Parse("1 M. N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c ef498f7ce55ad80dfa295825fa2fb45bd55ed97f index.txt").Build()
            };
            GitStatusChange[] next = new[]
            {
                GitStatusChange.Parse("1 .M N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c work1.txt").Build(),
                GitStatusChange.Parse("1 M. N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c ef498f7ce55ad80dfa295825fa2fb45bd55ed97f index.txt").Build()
            };
            repository.SetupSequence(m => m.ExecuteStatusAsync())
                .ReturnsAsync(new GitStatusResult(initial))
                .ReturnsAsync(new GitStatusResult(next));
            var stageOptions = new GitStagePageOptions(repository.Object);

            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            int workspaceCount = 0;
            foreach (object item in actor.WorkspaceFiles) { ++workspaceCount; }
            Assert.That(workspaceCount, Is.EqualTo(2));
            actor.WorkspaceFiles.MoveCurrentToLast();
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
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -1,4 +1,5 @@");
            builder.Feed(" line0");
            builder.Feed("+line1");
            builder.Feed("+line2");
            builder.Feed(" line3");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddIndex))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddIndex, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 2, 4));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.StageSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -1,2 +1,4 @@
 line0
+line1
+line2
 line3
".Replace("\r\n", "\n")));
        }

        [Test]
        public void StageSelectedTextCommandShouldTrimLines()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -1,4 +1,5 @@");
            builder.Feed(" line0 ");
            builder.Feed("+line1 ");
            builder.Feed("+    ");
            builder.Feed(" line3 ");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false,
                TrimLineEndings = true
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddIndex))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddIndex, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 0, 4));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.StageSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -1,2 +1,4 @@
 line0 
+line1
+
 line3 
".Replace("\r\n", "\n")));
        }

        [Test]
        public void StageSelectedTextCommandShouldCreatePatchWhenOnlyAddedLinesSelected()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -1,4 +1,5 @@");
            builder.Feed(" line0");
            builder.Feed("+line1");
            builder.Feed("+line2");
            builder.Feed(" line3");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddIndex))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddIndex, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 2, 3));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.StageSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -1,2 +1,4 @@
 line0
+line1
+line2
 line3
".Replace("\r\n", "\n")));
        }

        [Test]
        public void StageSelectedTextCommandShouldCreatePatchWhenEndHunkSelected()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -1,4 +1,5 @@");
            builder.Feed(" line0");
            builder.Feed("+line1");
            builder.Feed("+line2");
            builder.Feed(" line3");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddIndex))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddIndex, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 0, 4));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.StageSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -1,2 +1,4 @@
 line0
+line1
+line2
 line3
".Replace("\r\n", "\n")));
        }

        [Test]
        public void StageSelectedTextCommandShouldCreatePatchWhenFullHunkSelected()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -1,4 +1,5 @@");
            builder.Feed(" line0");
            builder.Feed("+line1");
            builder.Feed("+line2");
            builder.Feed(" line3");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddIndex))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddIndex, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 0, 4));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.StageSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -1,2 +1,4 @@
 line0
+line1
+line2
 line3
".Replace("\r\n", "\n")));
        }

        [Test]
        public void StageSelectedTextCommandShouldCreatePatchWhenCrossingHunks()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/advanced.txt b/advanced.txt");
            builder.Feed("index 9e8f761..914708a 100644");
            builder.Feed("--- a/advanced.txt");
            builder.Feed("+++ b/advanced.txt");
            builder.Feed("@@ -12,2 +12,3 @@ line1110");
            builder.Feed(" line12");
            builder.Feed("++line1213");
            builder.Feed(" line1312");
            builder.Feed("@@ -19 +20,3 @@ line18");
            builder.Feed(" line1925");
            builder.Feed("+");
            builder.Feed(" line");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e advanced.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddIndex))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddIndex, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 0, 7));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.StageSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/advanced.txt b/advanced.txt
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
".Replace("\r\n", "\n")));
        }

        [Test]
        public void StageSelectedTextCommandShouldCreatePatchWhenCrossingHunksAndEndIsOfTypeAdded()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/advanced.txt b/advanced.txt");
            builder.Feed("index 9e8f761..914708a 100644");
            builder.Feed("--- a/advanced.txt");
            builder.Feed("+++ b/advanced.txt");
            builder.Feed("@@ -12,2 +12,3 @@ line1110");
            builder.Feed(" line12");
            builder.Feed("++line1213");
            builder.Feed(" line1312");
            builder.Feed("@@ -19 +20,3 @@ line18");
            builder.Feed(" line1925");
            builder.Feed("+");
            builder.Feed("+line 12121");
            builder.Feed("\\ No newline at end of file");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e advanced.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddIndex))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddIndex, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 0, 7));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.StageSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/advanced.txt b/advanced.txt
--- a/advanced.txt
+++ b/advanced.txt
@@ -12,2 +12,3 @@
 line12
++line1213
 line1312
@@ -19,1 +20,3 @@
 line1925
+
+line 12121".Replace("\r\n", "\n")));
        }

        [Test]
        public void StageSelectedTextCommandShouldNotTrimRemovedLines()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -18,3 +18,3 @@");
            builder.Feed(" line1717");
            builder.Feed("-line18  ");
            builder.Feed("+line  19");
            builder.Feed(" line  24");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddIndex))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddIndex, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 2, 2));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.StageSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -18,3 +18,2 @@
 line1717
-line18  
 line  24
".Replace("\r\n", "\n")));
        }

        [Test]
        public void StageSelectedTextCommandShouldCreatePatchWhenAddedAndRemovedLines()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -18,4 +18,8 @@");
            builder.Feed(" line1717");
            builder.Feed("-line18  ");
            builder.Feed(" line  18");
            builder.Feed("+line  19");
            builder.Feed("+line  20");
            builder.Feed("+line  21");
            builder.Feed("+line  22");
            builder.Feed("+line  23");
            builder.Feed(" line  24");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddIndex))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddIndex, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 5, 7));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.StageSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -20,2 +20,5 @@
 line  18
+line  20
+line  21
+line  22
 line  24
".Replace("\r\n", "\n")));
        }

        [Test]
        public void StageSelectedTextCommandShouldCreatePatchWhenMixedSelection()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -4,16 +4,22 @@");
            builder.Feed(" line 4 4");
            builder.Feed(" line 5 5");
            builder.Feed(" line 6 6");
            builder.Feed("-line 7  ");
            builder.Feed(" line 8 7");
            builder.Feed(" line 9 8");
            builder.Feed(" line10 9");
            builder.Feed(" line1110");
            builder.Feed("-line12  ");
            builder.Feed("+line  11");
            builder.Feed(" line1312");
            builder.Feed(" line1413");
            builder.Feed("+line  14");
            builder.Feed(" line1515");
            builder.Feed(" line1616");
            builder.Feed(" line1717");
            builder.Feed("-line18  ");
            builder.Feed("+line  18");
            builder.Feed("+line  19");
            builder.Feed("+line  20");
            builder.Feed("+line  21");
            builder.Feed("+line  22");
            builder.Feed("+line  23");
            builder.Feed("+line  24");
            builder.Feed(" line1925");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddIndex))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddIndex, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 17, 23));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.StageSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
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
".Replace("\r\n", "\n")));
        }

        [Test]
        public void StageSelectedTextCommandShouldRemoveOneSelectedLine()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -3,8 +3,8 @@");
            builder.Feed("     \"line1\": false,");
            builder.Feed("     \"line2\": true,");
            builder.Feed("     \"line3\": {");
            builder.Feed("-      \"line4\": true,");
            builder.Feed("-      \"line5\": 50");
            builder.Feed("+      \"line4\": false,");
            builder.Feed("+      \"line5\": 0");
            builder.Feed("     }");
            builder.Feed("   }");
            builder.Feed(" }");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddIndex))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddIndex, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 4, 4));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.StageSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -5,3 +5,2 @@
     ""line3"": {
-      ""line4"": true,
       ""line5"": 50
".Replace("\r\n", "\n")));
        }

        [Test]
        public void ResetSelectedTextCommandShouldCreatePatchWhenMixedSelection()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -10,10 +10,17 @@");
            builder.Feed(" line10 9");
            builder.Feed(" line1110");
            builder.Feed(" line12");
            builder.Feed("++line1213");
            builder.Feed(" line1312");
            builder.Feed(" line1413");
            builder.Feed(" line1515");
            builder.Feed(" line1616");
            builder.Feed(" line1717");
            builder.Feed("-line18  ");
            builder.Feed("+line  18");
            builder.Feed("+line  19");
            builder.Feed("+line  20");
            builder.Feed("+line  21");
            builder.Feed("+line  22");
            builder.Feed("+line  23");
            builder.Feed("+line  24");
            builder.Feed(" line1925");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 M. N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.ResetIndex))
                .Returns(new DiffHunkLineVisitor(PatchMode.ResetIndex, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 13, 15));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.IndexFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.IndexFiles.CurrentItem;
            actor.ResetSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -20,5 +20,2 @@
 line  19
-line  20
-line  21
-line  22
 line  23
".Replace("\r\n", "\n")));
        }

        [Test]
        public void ResetSelectedTextCommandShouldResetRemovedLinesFromWorkspace()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -16,8 +16,11 @@ line1413");
            builder.Feed(" line1515");
            builder.Feed(" line1616");
            builder.Feed(" line1717");
            builder.Feed("-line18  ");
            builder.Feed("+line  18");
            builder.Feed("+line  19");
            builder.Feed(" line  20");
            builder.Feed(" line  21");
            builder.Feed(" line  22");
            builder.Feed("+line  23");
            builder.Feed("+line  24");
            builder.Feed(" line1925");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddWorkspace))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddWorkspace, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 4, 5));
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.ResetWorkspace))
                .Returns(new DiffHunkLineVisitor(PatchMode.ResetWorkspace, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 4, 4));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.ResetSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -18,2 +18,3 @@
 line1717
+line18  
 line  18
".Replace("\r\n", "\n")));
        }

        [Test]
        public void ResetSelectedTextCommandShouldResetRemovedAndAddedLines()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -16,8 +16,11 @@ line1413");
            builder.Feed(" line1515");
            builder.Feed(" line1616");
            builder.Feed(" line1717");
            builder.Feed("-line18  ");
            builder.Feed("+line  18");
            builder.Feed("+line  19");
            builder.Feed(" line  20");
            builder.Feed(" line  21");
            builder.Feed(" line  22");
            builder.Feed("+line  23");
            builder.Feed("+line  24");
            builder.Feed(" line1925");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddWorkspace))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddWorkspace, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 4, 5));
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.ResetWorkspace))
                .Returns(new DiffHunkLineVisitor(PatchMode.ResetWorkspace, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 4, 5));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.ResetSelectedTextCommand.Execute(document.Object);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Once);
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -18,3 +18,3 @@
 line1717
+line18  
-line  18
 line  19
".Replace("\r\n", "\n")));
        }

        [Test]
        public void ResetSelectedTextCommandShouldCreateUndoPatch()
        {
            string patchText = null;
            var repository = new Mock<IGitRepository>();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -16,8 +16,11 @@ line1413");
            builder.Feed(" line1515");
            builder.Feed(" line1616");
            builder.Feed(" line1717");
            builder.Feed("-line18  ");
            builder.Feed("+line  18");
            builder.Feed("+line  19");
            builder.Feed(" line  20");
            builder.Feed(" line  21");
            builder.Feed(" line  22");
            builder.Feed("+line  23");
            builder.Feed("+line  24");
            builder.Feed(" line1925");
            GitDiffResult result = builder.Build();
            repository
                .Setup(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()))
                .Callback<GitPatch>(patch => patchText = patch.Writer.ToString())
                .Returns(Task.CompletedTask);

            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            GitStatusChange[] initial = new[] { change };

            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(initial));

            var stageOptions = new GitStagePageOptions(repository.Object);
            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var watchProvider = new Mock<IGitRepositoryWatcherProvider>();
            var watch = new Mock<IRepositoryWatcher>();
            watchProvider
                .Setup(m => m.PrepareWatchRepositoryChanges(repository.Object))
                .Returns(watch.Object);
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();
            options.Setup(m => m.CurrentValue).Returns(new GitStageOptions
            {
                ShowSpacesAsDots = false
            });

            var document = new Mock<IHunkLineVisitorProvider>();
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.AddWorkspace))
                .Returns(new DiffHunkLineVisitor(PatchMode.AddWorkspace, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 4, 5));
            document
                .Setup(m => m.GetHunkVisitor(PatchMode.ResetWorkspace))
                .Returns(new DiffHunkLineVisitor(PatchMode.ResetWorkspace, result.Hunks.SelectMany(hunk => new[] { hunk.Header }.Concat(hunk.Lines)), 4, 5));

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                watchProvider.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            actor.WorkspaceFiles.MoveCurrentToFirst();
            actor.SelectedChange = (StatusChangeViewModel)actor.WorkspaceFiles.CurrentItem;
            actor.ResetSelectedTextCommand.Execute(document.Object);
            actor.UndoPatchCommand.Execute(null);
            repository.Verify(m => m.ExecuteApplyAsync(It.IsAny<GitPatch>()), Times.Exactly(2));
            Assert.That(patchText.Replace("\r\n", "\n"), Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -18,3 +18,3 @@
 line1717
-line18  
+line  18
 line  19
".Replace("\r\n", "\n")));
        }
    }
}
