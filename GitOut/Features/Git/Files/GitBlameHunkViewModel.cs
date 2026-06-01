using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using GitOut.Features.Text;

namespace GitOut.Features.Git.Files;

public class GitBlameHunkViewModel
{
    private const string FontFamilyName = "Consolas sans-serif";

    public GitBlameHunkViewModel(
        GitBlameHunk hunk,
        FlowDocument document,
        IEnumerable<int> lineNumbers
    )
    {
        CommitId = hunk.CommitId;
        Author = hunk.Author;
        AuthorEmail = hunk.AuthorEmail;
        AuthorDate = hunk.AuthorDate;
        Summary = hunk.Summary;
        Document = document;
        LineNumbers = lineNumbers.ToList();
    }

    public GitCommitId CommitId { get; }
    public string Author { get; }
    public string AuthorEmail { get; }
    public DateTimeOffset AuthorDate { get; }
    public string Summary { get; }

    public IEnumerable<int> LineNumbers { get; }
    public FlowDocument Document { get; }

    public static GitBlameHunkViewModel Parse(GitBlameHunk hunk, ISyntaxHighlighter highlighter)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily(FontFamilyName),
            FontSize = 12,
            PagePadding = new Thickness(0),
            PageWidth = 5000,
        };

        IEnumerable<Paragraph> highlighted = highlighter.Highlight(
            hunk.Lines.Select(line => line.Content),
            new EmptyLineDecorator()
        );

        foreach (Paragraph p in highlighted)
        {
            document.Blocks.Add(p);
        }

        return new GitBlameHunkViewModel(hunk, document, hunk.Lines.Select(l => l.FinalLineNumber));
    }

    private class EmptyLineDecorator : ILineDecorator
    {
        public void Decorate(TextElement paragraph, int lineNumber)
        {
            // No background decoration for blame lines
        }
    }
}
