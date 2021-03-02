using System.Collections.Generic;

namespace GitOut.Features.Diagnostics
{
    public interface IProcessOptionsBuilder
    {
        IProcessOptionsBuilder Append(string argument);
        IProcessOptionsBuilder AppendRange(params string[] collection);
        IProcessOptionsBuilder AppendRange(IEnumerable<string> collection);
        ProcessOptions Build();
    }
}
