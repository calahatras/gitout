using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using FakeItEasy;
using GitOut.Features.Diagnostics;
using GitOut.Features.Git.Diagnostics;
using GitOut.Features.Git.Storage;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using NUnit.Framework;

namespace GitOut.Features.Git.RepositoryList;

public class RepositoryListViewModelTest
{
#pragma warning disable CS8618
    private INavigationService navigation;
    private IGitRepositoryStorage storage;
    private IGitRepositoryFactory repositoryFactory;
    private ISnackbarService snack;
    private IProcessFactory<IGitProcess> processFactory;
    private RepositoryListViewModel viewModel;
#pragma warning restore CS8618

    [SetUp]
    [MemberNotNull(
        nameof(navigation),
        nameof(storage),
        nameof(repositoryFactory),
        nameof(snack),
        nameof(processFactory),
        nameof(viewModel)
    )]
    public void Setup()
    {
        navigation = A.Fake<INavigationService>();
        storage = A.Fake<IGitRepositoryStorage>();
        _ = A.CallTo(() => storage.Repositories).Returns(Observable.Empty<IReadOnlyCollection<IGitRepository>>());
        repositoryFactory = A.Fake<IGitRepositoryFactory>();
        snack = A.Fake<ISnackbarService>();
        processFactory = A.Fake<IProcessFactory<IGitProcess>>();

        viewModel = new RepositoryListViewModel(
            navigation,
            storage,
            repositoryFactory,
            snack,
            processFactory
        );
    }

    [Test]
    public void CloneCommandShouldSetActiveCloneDirectoryAndResetFields()
    {
        viewModel.CloneUrl = "https://github.com/foo/bar.git";
        viewModel.CloneBranch = "feature";

        viewModel.CloneCommand.Execute("C:\\repos");

        Assert.That(viewModel.ActiveCloneDirectory, Is.EqualTo("C:\\repos"));
        Assert.That(viewModel.CloneUrl, Is.Empty);
        Assert.That(viewModel.CloneBranch, Is.Empty);
    }

    [Test]
    public void CloneCommandShouldToggleOffWhenCalledWithActiveDirectory()
    {
        viewModel.CloneCommand.Execute("C:\\repos");
        Assert.That(viewModel.ActiveCloneDirectory, Is.EqualTo("C:\\repos"));

        viewModel.CloneCommand.Execute("C:\\repos");
        Assert.That(viewModel.ActiveCloneDirectory, Is.Null);
    }

    [Test]
    public void CancelCloneCommandShouldClearActiveCloneDirectory()
    {
        viewModel.CloneCommand.Execute("C:\\repos");
        Assert.That(viewModel.ActiveCloneDirectory, Is.EqualTo("C:\\repos"));

        viewModel.CancelCloneCommand.Execute(null);

        Assert.That(viewModel.ActiveCloneDirectory, Is.Null);
    }

    [Test]
    public void ExecuteCloneCommandCanExecuteShouldDependOnDirectoryAndUrl()
    {
        Assert.That(viewModel.ExecuteCloneCommand.CanExecute(null), Is.False);

        viewModel.ActiveCloneDirectory = "C:\\repos";
        Assert.That(viewModel.ExecuteCloneCommand.CanExecute(null), Is.False);

        viewModel.CloneUrl = "https://github.com/foo/bar.git";
        Assert.That(viewModel.ExecuteCloneCommand.CanExecute(null), Is.True);
    }

    [Test]
    public void CloneDestinationHintShouldReflectRepositoryNameFromUrl()
    {
        viewModel.ActiveCloneDirectory = "C:\\repos";
        Assert.That(viewModel.CloneDestinationHint, Is.EqualTo("Clone to C:\\repos"));

        viewModel.CloneUrl = "https://github.com/owner/my-project.git";
        Assert.That(viewModel.CloneDestinationHint, Does.EndWith("my-project"));
    }
}
