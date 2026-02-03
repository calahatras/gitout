using System.Collections.Generic;
using System.Linq;

namespace GitOut.Features.Git;

public class GitStatusResult
{
    public GitStatusResult(IEnumerable<GitStatusChange> changes) =>
        Changes = changes.ToList().AsReadOnly();

    public IReadOnlyCollection<GitStatusChange> Changes { get; }
}
