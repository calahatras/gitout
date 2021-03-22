using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace GitOut.Features.Text
{
    public class CSharpSyntaxHighlighter : ISyntaxHighlighter
    {
        private static readonly Thickness ZeroThickness = new Thickness(0);

        private static readonly string[] Keywords = new[]
        {
            "public",
            "protected",
            "private",
            "internal",
            "partial",
            "abstract",
            "virtual",
            "override",
            "base",
            "static",
            "sealed",
            "namespace",
            "using",
            "readonly",
            "const",
            "class",
            "interface",
            "struct",
            "void",
            "async",
            "await",
            "get",
            "set",
            "event",
            "add",
            "remove",
            "ref",
            "out",
            "new",
            "is",
            "this",
            "params",
            "lock",
            "nameof",
            "typeof",
            "bool",
            "true",
            "false",
            "null",
            "string",
            "object",
            "int",
            "double",
            "short",
            "byte",
            "var",
            "where"
        };

        private static readonly string[] ControlKeywords = new[]
        {
            "return",
            "if",
            "else",
            "for",
            "foreach",
            "in",
            "while",
            "do",
            "switch",
            "case",
            "default",
            "continue",
            "break",
            "yield",
            "try",
            "catch",
            "finally",
            "throw",
        };

        private static readonly Regex CommentRegex = new Regex($"//.*$", RegexOptions.Compiled);
        private static readonly Regex KeywordRegex = new Regex($"\\b({string.Join("|", Keywords)})\\b", RegexOptions.Compiled);
        private static readonly Regex ControlKeywordRegex = new Regex($"\\b({string.Join("|", ControlKeywords)})\\b", RegexOptions.Compiled);
        private static readonly Regex StringRegex = new Regex("\"(.)+?(?<!\\\\)\"", RegexOptions.Compiled);

        public IEnumerable<Paragraph> Highlight(IEnumerable<string> document, ILineDecorator decorator) => document.Select((line, index) =>
        {
            var para = new Paragraph() { Margin = ZeroThickness };
            para.Inlines.AddRange(Tokenize(line));
            decorator.Decorate(para, index);
            return para;
        });

        private static IEnumerable<Run> Tokenize(string line)
        {
            if (line.Length == 0)
            {
                yield return new Run();
            }
            IReadOnlyCollection<Match> commentMatch = CommentRegex.Matches(line);
            IReadOnlyCollection<Match> keywordMatch = KeywordRegex.Matches(line);
            IReadOnlyCollection<Match> controlKeywordMatch = ControlKeywordRegex.Matches(line);
            IReadOnlyCollection<Match> stringMatch = StringRegex.Matches(line);

            // join collections and remove invalid (e.g. keywords in string)
            IEnumerable<IDecoratedMatch> matches = Join(
                line.Length,
                commentMatch.Select(match => new ColorAppliedMatch(match, CSharpSyntaxHighlighterOptions.CommentForegroundColor)),
                keywordMatch.Select(match => new ColorAppliedMatch(match, CSharpSyntaxHighlighterOptions.KeywordForegroundColor)),
                controlKeywordMatch.Select(match => new ColorAppliedMatch(match, CSharpSyntaxHighlighterOptions.ControlKeywordForegroundColor)),
                stringMatch.Select(match => new ColorAppliedMatch(match, CSharpSyntaxHighlighterOptions.StringForegroundColor))
            );
            foreach (IDecoratedMatch match in matches)
            {
                yield return match.Apply(line);
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
            public Run Apply(string line) => new Run(line[Offset..EndIndex]);
        }

        private class ColorAppliedMatch : IDecoratedMatch
        {
            private readonly Brush color;

            public ColorAppliedMatch(Match match, Brush color)
            {
                Index = match.Index;
                Length = match.Length;
                this.color = color;
            }

            public int Index { get; }
            public int Length { get; }

            public Run Apply(string line) => new Run(line[Index..(Index + Length)])
            {
                Foreground = color
            };
        }
    }
}
