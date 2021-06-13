using System.Collections.Generic;
using System.Threading.Tasks;
using GitOut.Features.IO;

namespace GitOut.Features.Collections
{
    public interface ILazyAsyncEnumerable<out T> : IEnumerable<T>
    {
        bool IsMaterialized { get; }
        ValueTask MaterializeAsync(RelativeDirectoryPath path);
    }
}
