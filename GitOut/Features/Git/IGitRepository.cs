using System.Collections.Generic;
using System.Threading.Tasks;
using GitOut.Features.IO;

namespace GitOut.Features.Git
{
    public interface IGitRepository
    {
        DirectoryPath WorkingDirectory { get; }
        string? Name { get; }

        Task<IEnumerable<GitHistoryEvent>> ExecuteLogAsync();
    }
}
