using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using FakeItEasy;
using NUnit.Framework;

namespace GitOut.Features.Text;

public class CSharpSyntaxHighlighterTest
{
    [Test]
    public void HighlightShouldHighlight()
    {
        string[] lines = ["public void Main() {"];

        ILineDecorator decorator = A.Fake<ILineDecorator>();

        var actor = new CSharpSyntaxHighlighter();
        IEnumerable<Paragraph> document = actor.Highlight(lines, decorator);
        Assert.That(document, Is.Not.Null);
    }

    [Test]
    public void HighlightShouldFindStrings()
    {
        string[] lines =
        [
            "\"this is a string\"",
            "string x = \"a string value\"",
            "<div class=\"d-flex\" data.attr=\"1\">",
            "string a = \"escaped \\\"string\\\"\";",
        ];
        ILineDecorator decorator = A.Fake<ILineDecorator>();

        var actor = new CSharpSyntaxHighlighter();
        IList<Paragraph> document = actor.Highlight(lines, decorator).ToList();

        Assert.That(
            document[0]
                .Inlines.OfType<Run>()
                .Where(run =>
                    run.Foreground == CSharpSyntaxHighlighterOptions.StringForegroundColor
                )
                .Select(run => run.Text.Trim('"')),
            Is.EquivalentTo(new[] { "this is a string" })
        );
        Assert.That(
            document[1]
                .Inlines.OfType<Run>()
                .Where(run =>
                    run.Foreground == CSharpSyntaxHighlighterOptions.StringForegroundColor
                )
                .Select(run => run.Text.Trim('"')),
            Is.EquivalentTo(new[] { "a string value" })
        );
        Assert.That(
            document[2]
                .Inlines.OfType<Run>()
                .Where(run =>
                    run.Foreground == CSharpSyntaxHighlighterOptions.StringForegroundColor
                )
                .Select(run => run.Text.Trim('"')),
            Is.EquivalentTo(new[] { "d-flex", "1" })
        );
        Assert.That(
            document[3]
                .Inlines.OfType<Run>()
                .Where(run =>
                    run.Foreground == CSharpSyntaxHighlighterOptions.StringForegroundColor
                )
                .Select(run => run.Text),
            Is.EquivalentTo(new[] { "\"escaped \\\"string\\\"\"" })
        );
    }

    [Test]
    public void HighlightShouldFindKeywords()
    {
        string[] lines =
        [
            "\"this is a string\"",
            "string x = \"a string value\"",
            "var empty = \"\"",
        ];

        ILineDecorator decorator = A.Fake<ILineDecorator>();

        var actor = new CSharpSyntaxHighlighter();
        IList<Paragraph> document = actor.Highlight(lines, decorator).ToList();

        Assert.That(((Run)document[1].Inlines.FirstInline).Text, Is.EqualTo("string"));
        Assert.That(
            document[1].Inlines.FirstInline.Foreground,
            Is.EqualTo(CSharpSyntaxHighlighterOptions.KeywordForegroundColor)
        );
        Assert.That(((Run)document[2].Inlines.FirstInline).Text, Is.EqualTo("var"));
        Assert.That(
            document[2].Inlines.FirstInline.Foreground,
            Is.EqualTo(CSharpSyntaxHighlighterOptions.KeywordForegroundColor)
        );
    }
}
