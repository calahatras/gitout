using System;
using System.Linq;
using FakeItEasy;
using GitOut.Features.Git.Storage;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Wpf;
using NUnit.Framework;

namespace GitOut.Features.Git.Hooks;

public class GitHooksViewModelTest
{
    [Test]
    public void Constructor_InitializesExpectedProperties()
    {
        ITitleService title = A.Fake<ITitleService>();
        ISnackbarService snacks = A.Fake<ISnackbarService>();
        IGitRepositoryStorage storage = A.Fake<IGitRepositoryStorage>();
        IGitHookManager hookManager = A.Fake<IGitHookManager>();

        IGitRepository repo = A.Fake<IGitRepository>();
        _ = A.CallTo(() => repo.Name).Returns("GitOut");
        var options = GitHooksPageOptions.OpenRepository(repo);

        var viewModel = new GitHooksViewModel(title, snacks, storage, hookManager, options);

        Assert.That(viewModel.AvailableHooks.Count(), Is.GreaterThan(0));
        _ = A.CallToSet(() => title.Title).To("Hooks - GitOut").MustHaveHappened();
    }
}
