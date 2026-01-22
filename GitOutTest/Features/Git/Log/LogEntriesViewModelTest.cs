#pragma warning disable CA1506
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using GitOut.Features.Git.Files;
using GitOut.Features.IO;
using GitOut.Features.Material.Snackbar;
using NUnit.Framework;

namespace GitOut.Features.Git.Log
{
    public class LogEntriesViewModelTest
    {
        [Test]
        public async Task AllFilesShouldBuildTreeAsync()
        {
            var hash = GitFileId.FromHash("f665b5a0faac191d8558ea8af26c8aa478be7c48");
            var parent = GitFileId.FromHash("23307fe3ad484c6086a2f6a1f3387087a29217cb");
            GitHistoryEvent root = GitHistoryEvent
                .Builder()
                .ParseHash(hash.ToString() + parent.ToString())
                .ParseDate(1613333029)
                .ParseAuthorEmail("user@example.com")
                .ParseAuthorName("User")
                .ParseSubject("refactor(log): add test")
                .Build();

            ICollection<GitFileEntry> entries =
            [
                GitFileEntry.Parse("100644 blob 96d80cd6c4e7158dbebd0849f4fb7ce513e5828c\tf.txt"),
                GitFileEntry.Parse(
                    "100644 blob 96d80cd6c4e7158dbebd0849f4fb7ce513e5828d\tA/Subfolder/For/a.txt"
                ),
                GitFileEntry.Parse(
                    "100644 blob 96d80cd6c4e7158dbebd0849f4fb7ce513e5828e\tA/Subfolder/For/b.txt"
                ),
            ];

            IGitRepository repository = A.Fake<IGitRepository>();
            IGitRepositoryNotifier notifier = A.Fake<IGitRepositoryNotifier>();
            Captured<DiffOptions> capturedDiffOptions = A.Captured<DiffOptions>();
            A.CallTo(() => repository.ListTreeAsync(root.Id, capturedDiffOptions._))
                .Returns(entries.ToAsyncEnumerable());
            ISnackbarService snack = A.Fake<ISnackbarService>();

            var actor = LogEntriesViewModel.CreateContext(
                new List<GitHistoryEvent>([root]),
                repository,
                notifier,
                snack,
                LogRevisionViewMode.CurrentRevision
            );
            Assert.That(actor, Is.Not.Null);
            await actor!.AllFiles.MaterializeAsync(RelativeDirectoryPath.Root);

            Assert.That(actor.RootFiles.OfType<IGitFileEntryViewModel>().Count(), Is.EqualTo(2));
            Assert.That(
                actor.RootFiles.OfType<IGitDirectoryEntryViewModel>().Count(),
                Is.EqualTo(1)
            );
        }
    }
}
