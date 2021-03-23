using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitOut.Features.Diagnostics;

namespace GitOut.Features.Git.Diagnostics
{
    public interface IGitProcess
    {
        Task<ProcessEventArgs> ExecuteAsync(CancellationToken cancellationToken = default);
        Task<ProcessEventArgs> ExecuteAsync(StringBuilder writer, CancellationToken cancellationToken = default);
        IAsyncEnumerable<string> ReadLinesAsync(CancellationToken cancellationToken = default);
    }
}
