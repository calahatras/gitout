using System.Collections.Generic;
using System.Threading;

namespace GitOut.Features.Git.Hooks;

public interface IShellProvider
{
    IAsyncEnumerable<ShellPath> FindAvailableShellsAsync(
        CancellationToken cancellationToken = default
    );
}
