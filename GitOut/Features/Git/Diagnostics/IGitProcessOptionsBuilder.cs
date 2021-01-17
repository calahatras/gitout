using System.Collections.Generic;

namespace GitOut.Features.Git.Diagnostics
{
    public interface IGitProcessOptionsBuilder
    {
        IGitProcessOptionsBuilder Append(string argument);
        IGitProcessOptionsBuilder AppendRange(IEnumerable<string>? argument);
        GitProcessOptions Build();
    }
}
