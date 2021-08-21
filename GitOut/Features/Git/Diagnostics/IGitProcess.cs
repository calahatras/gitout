using System.Collections.Generic;
using System.IO;
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
        Task<Stream> ReadStreamAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<string> ReadLinesAsync(CancellationToken cancellationToken = default);
    }
}
