using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using Moq;
using NUnit.Framework;

namespace GitOut.Features.Text
{
    public class CSharpSyntaxHighlighterTest
    {
        [Test]
        public void HighlightShouldHighlight()
        {
            string[] lines = new[]
            {
                "public void Main() {",

            };

            var decorator = new Mock<ILineDecorator>();

            var actor = new CSharpSyntaxHighlighter();
            IEnumerable<Paragraph> document = actor.Highlight(lines, decorator.Object);
            Assert.That(document, Is.Not.Null);
        }

        [Test]
        public void HighlightShouldFindStrings()
        {
            string[] lines = new[]
            {
                "\"this is a string\"",
                "string x = \"a string value\"",
                "<div class=\"d-flex\" data.attr=\"1\">",
                "string a = \"escaped \\\"string\\\"\";"
            };
            var decorator = new Mock<ILineDecorator>();

            var actor = new CSharpSyntaxHighlighter();
            IList<Paragraph> document = actor.Highlight(lines, decorator.Object).ToList();

            CollectionAssert.AreEqual(
                new[] { "this is a string" },
                document[0].Inlines.OfType<Run>().Where(run => run.Foreground == CSharpSyntaxHighlighterOptions.StringForegroundColor).Select(run => run.Text.Trim('"'))
            );
            CollectionAssert.AreEqual(
                new[] { "a string value" },
                document[1].Inlines.OfType<Run>().Where(run => run.Foreground == CSharpSyntaxHighlighterOptions.StringForegroundColor).Select(run => run.Text.Trim('"'))
            );
            CollectionAssert.AreEqual(
                new[] { "d-flex", "1" },
                document[2].Inlines.OfType<Run>().Where(run => run.Foreground == CSharpSyntaxHighlighterOptions.StringForegroundColor).Select(run => run.Text.Trim('"'))
            );
            CollectionAssert.AreEqual(
                new[] { "\"escaped \\\"string\\\"\"" },
                document[3].Inlines.OfType<Run>().Where(run => run.Foreground == CSharpSyntaxHighlighterOptions.StringForegroundColor).Select(run => run.Text)
            );
        }

        [Test]
        public void HighlightShouldFindKeywords()
        {
            string[] lines = new[]
            {
                "\"this is a string\"",
                "string x = \"a string value\"",
                "var empty = \"\""
            };

            var decorator = new Mock<ILineDecorator>();

            var actor = new CSharpSyntaxHighlighter();
            IList<Paragraph> document = actor.Highlight(lines, decorator.Object).ToList();

            Assert.That(((Run)document[1].Inlines.FirstInline).Text, Is.EqualTo("string"));
            Assert.That(document[1].Inlines.FirstInline.Foreground, Is.EqualTo(CSharpSyntaxHighlighterOptions.KeywordForegroundColor));
            Assert.That(((Run)document[2].Inlines.FirstInline).Text, Is.EqualTo("var"));
            Assert.That(document[2].Inlines.FirstInline.Foreground, Is.EqualTo(CSharpSyntaxHighlighterOptions.KeywordForegroundColor));
        }
    }
}
