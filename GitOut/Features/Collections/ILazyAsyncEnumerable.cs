using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitOut.Features.Collections
{
    public interface ILazyAsyncEnumerable<out T, TArg> : IEnumerable<T>
    {
        bool IsMaterialized { get; }
        ValueTask MaterializeAsync(TArg path);
    }
}
