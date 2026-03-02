#pragma warning disable CS8618
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using FakeItEasy;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;
using NUnit.Framework;

namespace GitOut.Features.Text.Editor;

public class TextEditorViewModelTest
{
    private INavigationService navigation;
    private ITitleService title;
    private ISnackbarService snackbar;
    private TextEditorOptions options;

    [SetUp]
    public void SetUp()
    {
        navigation = A.Fake<INavigationService>();
        title = A.Fake<ITitleService>();
        snackbar = A.Fake<ISnackbarService>();
        options = new TextEditorOptions("test.txt", "Test title");
        A.CallTo(() => navigation.GetOptions<TextEditorOptions>(typeof(TextEditorPage).FullName!))
            .Returns(options);
    }

    [Test]
    public void Constructor_SetsTitle()
    {
        var viewModel = new TextEditorViewModel(navigation, title, snackbar);
        Assert.That(title.Title, Is.EqualTo("Test title"));
    }

    [Test]
    public void TextProperty_SetProperty_SetsHasChangesToTrue()
    {
        var viewModel = new TextEditorViewModel(navigation, title, snackbar);
        viewModel.TextContent = "New text";

        Assert.That(viewModel.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void TextProperty_NotifiesPropertyChanged()
    {
        var viewModel = new TextEditorViewModel(navigation, title, snackbar);
        var propertyChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(TextEditorViewModel.TextContent))
            {
                propertyChanged = true;
            }
        };

        viewModel.TextContent = "New text";

        Assert.That(propertyChanged, Is.True);
    }
}
