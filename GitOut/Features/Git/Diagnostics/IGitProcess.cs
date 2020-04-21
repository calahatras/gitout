using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitOut.Features.Git.Diagnostics
{
    public interface IGitProcess
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
        Task ExecuteAsync(StringBuilder writer, CancellationToken cancellationToken = default);
        IAsyncEnumerable<string> ReadLinesAsync(CancellationToken cancellationToken = default);
    }
}
