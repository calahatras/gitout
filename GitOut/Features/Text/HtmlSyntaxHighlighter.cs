using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace GitOut.Features.Text
{
    public class HtmlSyntaxHighlighter : ISyntaxHighlighter
    {
        private static readonly Thickness ZeroThickness = new(0);

        private static readonly Regex ElementRegex = new(@"<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex AttributeRegex = new(@"\b\w+(?=\=)", RegexOptions.Compiled);
        private static readonly Regex StringRegex = new("\"(.)+?(?<!\\\\)\"", RegexOptions.Compiled);
        private static readonly Regex BoundAttributeRegex = new(@"\[.*?\]|\(.*?\)", RegexOptions.Compiled);

        public IEnumerable<Paragraph> Highlight(IEnumerable<string> document, ILineDecorator decorator) => document.Select((line, index) =>
        {
            var para = new Paragraph() { Margin = ZeroThickness };
            para.Inlines.AddRange(Tokenize(line));
            decorator.Decorate(para, index);
            return para;
        });

        private static IEnumerable<Inline> Tokenize(string line)
        {
            if (line.Length == 0)
            {
                yield return new Run();
            }
            IReadOnlyCollection<Match> elementMatch = ElementRegex.Matches(line);
            IReadOnlyCollection<Match> attributeMatch = AttributeRegex.Matches(line);
            IReadOnlyCollection<Match> stringMatch = StringRegex.Matches(line);
            IReadOnlyCollection<Match> boundAttributeMatch = BoundAttributeRegex.Matches(line);

            // join collections and remove invalid (e.g. attributes in string)
            IEnumerable<IDecoratedMatch> matches = Join(
                line.Length,
                elementMatch.Select(match => new ColorAppliedMatch(match, HtmlSyntaxHighlighterOptions.ElementForegroundColor)),
                attributeMatch.Select(match => new ColorAppliedMatch(match, HtmlSyntaxHighlighterOptions.AttributeForegroundColor)),
                stringMatch.Select(match => new ColorAppliedMatch(match, HtmlSyntaxHighlighterOptions.StringForegroundColor)),
                boundAttributeMatch.Select(match => new ColorAppliedMatch(match, HtmlSyntaxHighlighterOptions.BoundStringForegroundColor))
            );
            foreach (IDecoratedMatch match in matches)
            {
                foreach (Run inline in match.Apply(line))
                {
                    yield return inline;
                }
            }
        }

        private static IEnumerable<IDecoratedMatch> Join(int length, params IEnumerable<ColorAppliedMatch>[] collections)
        {
            IEnumerable<ColorAppliedMatch> ordered = collections
                .SelectMany(e => e)
                .OrderBy(match => match.Index);

            int currentOffset = 0;
            foreach (ColorAppliedMatch m in ordered)
            {
                if (m.Index < currentOffset)
                {
                    continue;
                }
                if (m.Index > currentOffset)
                {
                    yield return new UndecoratedMatch(currentOffset, m.Index);
                }
                yield return m;
                currentOffset = m.Index + m.Length;
            }
            if (currentOffset < length)
            {
                yield return new UndecoratedMatch(currentOffset, length);
            }
        }

        private class UndecoratedMatch : IDecoratedMatch
        {
            public UndecoratedMatch(int offset, int endIndex)
            {
                Offset = offset;
                EndIndex = endIndex;
            }

            public int Offset { get; }
            public int EndIndex { get; }
            public IEnumerable<Run> Apply(string line) => new List<Run> { new(line[Offset..EndIndex]) };
        }

        private class ColorAppliedMatch : IDecoratedMatch
        {
            private readonly Brush color;
            private readonly Match match;

            public ColorAppliedMatch(Match match, Brush color)
            {
                this.match = match;
                Index = match.Index;
                Length = match.Length;
                this.color = color;
            }

            public int Index { get; }
            public int Length { get; }

            public IEnumerable<Run> Apply(string line)
            {
                string matchedText = line[Index..(Index + Length)];
                if (ElementRegex.IsMatch(matchedText))
                {
                    var inlines = new List<Run>();
                    string[] parts = matchedText.Split(' ');
                    inlines.Add(new Run(parts[0]) { Foreground = HtmlSyntaxHighlighterOptions.ElementForegroundColor });
                    for (int i = 1; i < parts.Length; i++)
                    {
                        if (AttributeRegex.IsMatch(parts[i]))
                        {
                            inlines.Add(new Run(" " + parts[i]) { Foreground = HtmlSyntaxHighlighterOptions.AttributeForegroundColor });
                        }
                        else if (StringRegex.IsMatch(parts[i]))
                        {
                            inlines.Add(new Run(" " + parts[i]) { Foreground = HtmlSyntaxHighlighterOptions.StringForegroundColor });
                        }
                        else if (BoundAttributeRegex.IsMatch(parts[i]))
                        {
                            inlines.Add(new Run(" " + parts[i]) { Foreground = HtmlSyntaxHighlighterOptions.BoundStringForegroundColor });
                        }
                        else
                        {
                            inlines.Add(new Run(" " + parts[i]) { Foreground = color });
                        }
                    }
                    return inlines;
                }
                return new List<Run> { new(matchedText) { Foreground = color } };
            }
        }
    }
}