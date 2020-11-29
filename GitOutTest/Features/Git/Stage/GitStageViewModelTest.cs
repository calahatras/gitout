using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
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
        public void RefreshShouldHandleRenamedFileWithWorkspaceChanges()
        {
            var repository = new Mock<IGitRepository>();
            var changes = new List<GitStatusChange>
            {
                GitStatusChange.Parse("2 RM N... 100644 100644 100644 aea670e83087b8015c431146dc9812a04b818a79 aea670e83087b8015c431146dc9812a04b818a79 R100 node3.txt node2.txt").MergedFrom("node2.txt").Build()
            };
            repository.Setup(m => m.ExecuteStatusAsync()).ReturnsAsync(new GitStatusResult(changes));
            var stageOptions = new GitStagePageOptions(repository.Object);

            var navigation = new Mock<INavigationService>();
            navigation.Setup(m => m.GetOptions<GitStagePageOptions>(typeof(GitStagePage).FullName)).Returns(stageOptions);
            var title = new Mock<ITitleService>();
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                snack.Object,
                options.Object
            );

            var waitHandle = new ManualResetEventSlim(false);

            IList<StatusChangeViewModel> actual = new List<StatusChangeViewModel>();
            actor.WorkspaceFiles.CollectionChanged += (o, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        {
                            foreach (StatusChangeViewModel item in e.NewItems)
                            {
                                actual.Add(item);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        {
                            foreach (StatusChangeViewModel item in e.OldItems)
                            {
                                actual.Remove(item);
                            }
                        }
                        break;
                }
                waitHandle.Set();
            };

            actor.Navigated(NavigationType.Initial);

            Assert.That(waitHandle.Wait(1), Is.True);
            Assert.That(actor.IndexFiles.IsEmpty, Is.False);
            Assert.That(actor.WorkspaceFiles.IsEmpty, Is.False);
            Assert.That(actual.Count, Is.EqualTo(1));
        }

        [Test]
        public void RefreshShouldRemoveWorkspaceFileAfterStageRefresh()
        {
            var repository = new Mock<IGitRepository>();
            var initial = new List<GitStatusChange>
            {
                GitStatusChange.Parse("1 .M N... 100644 100644 100644 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c 8ccafb210a2d79746acc7ac06ed509f8e87ddf4c empty.txt").Build()
            };
            var staged = new List<GitStatusChange>
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
            var snack = new Mock<ISnackbarService>();
            var options = new Mock<IOptionsMonitor<GitStageOptions>>();

            var actor = new GitStageViewModel(
                navigation.Object,
                title.Object,
                snack.Object,
                options.Object
            );

            // initialize workspace files with initial change
            actor.Navigated(NavigationType.Initial);
            int count = 0;
            foreach (object item in actor.WorkspaceFiles) { ++count; }
            Assert.That(count, Is.EqualTo(1));

            var waitHandle = new ManualResetEventSlim(false);
            IList<StatusChangeViewModel> actual = new List<StatusChangeViewModel>();
            actor.IndexFiles.CollectionChanged += (o, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        {
                            foreach (StatusChangeViewModel item in e.NewItems)
                            {
                                actual.Add(item);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        {
                            foreach (StatusChangeViewModel item in e.OldItems)
                            {
                                actual.Remove(item);
                            }
                        }
                        break;
                }
                waitHandle.Set();
            };

            // force a status refresh by calling initial again
            actor.Navigated(NavigationType.Initial);
            Assert.That(waitHandle.Wait(1), Is.True);

            Assert.That(actor.IndexFiles.IsEmpty, Is.False);
            Assert.That(actor.WorkspaceFiles.IsEmpty, Is.True);
            Assert.That(actual.Count, Is.EqualTo(1));
        }
    }
}
