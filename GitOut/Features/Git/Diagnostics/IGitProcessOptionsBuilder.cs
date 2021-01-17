using System.Collections.Generic;

namespace GitOut.Features.Git.Diagnostics
{
    public interface IGitProcessOptionsBuilder
    {
        IGitProcessOptionsBuilder Append(string argument);
        IGitProcessOptionsBuilder AppendRange(params string[] collection);
        IGitProcessOptionsBuilder AppendRange(IEnumerable<string> collection);
        GitProcessOptions Build();
    }
}
