using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;

namespace GitOut.Features.Text;

public class PlainTextSyntaxHighlighter : ISyntaxHighlighter
{
    private static readonly Thickness ZeroThickness = new(0);

    public IEnumerable<Paragraph> Highlight(
        IEnumerable<string> document,
        ILineDecorator decorator
    ) =>
        document.Select(
            (line, index) =>
            {
                var para = new Paragraph() { Margin = ZeroThickness };
                para.Inlines.Add(new Run(line));
                decorator.Decorate(para, index);
                return para;
            }
        );
}
