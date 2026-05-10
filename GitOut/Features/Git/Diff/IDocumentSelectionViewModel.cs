using System.Collections.Generic;
using System.Windows.Documents;

namespace GitOut.Features.Git.Diff;

public interface IDocumentSelectionViewModel
{
    IEnumerable<LineNumberViewModel> LineNumbers { get; }
    FlowDocument Document { get; }
    TextRange? Selection { get; set; }
}
