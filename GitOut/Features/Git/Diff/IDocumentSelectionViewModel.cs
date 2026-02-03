using System.Collections.Generic;
using System.Windows.Documents;

namespace GitOut.Features.Git.Diff;

public interface IDocumentSelectionViewModel
{
    public IEnumerable<LineNumberViewModel> LineNumbers { get; }
    public FlowDocument Document { get; }
    TextRange? Selection { get; set; }
}
