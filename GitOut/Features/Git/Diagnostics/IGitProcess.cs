using System.Collections.Generic;
using System.Threading;

namespace GitOut.Features.Git.Diagnostics
{
    public interface IGitProcess
    {
        IAsyncEnumerable<string> ReadLines(CancellationToken cancellationToken = default);
    }
}
