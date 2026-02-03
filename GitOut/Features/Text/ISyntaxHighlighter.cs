using System.Collections.Generic;
using System.Windows.Documents;

namespace GitOut.Features.Text;

public interface ISyntaxHighlighter
{
    IEnumerable<Paragraph> Highlight(IEnumerable<string> document, ILineDecorator decorator);
}
