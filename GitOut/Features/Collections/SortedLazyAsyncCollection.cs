using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitOut.Features.Collections
{
    public class SortedLazyAsyncCollection<T, TArg>
        : SortedObservableCollection<T>,
            ILazyAsyncEnumerable<T, TArg>
    {
        private readonly Func<TArg, IAsyncEnumerable<T>> factory;

        public SortedLazyAsyncCollection(
            Func<TArg, IAsyncEnumerable<T>> factory,
            Func<T, T, int> comparer
        )
            : base(comparer) => this.factory = factory;

        public bool IsMaterialized { get; private set; }

        public async ValueTask MaterializeAsync(TArg argument)
        {
            if (IsMaterialized)
            {
                return;
            }
            IsMaterialized = true;
            IAsyncEnumerable<T> enumerable = factory(argument);
            await foreach (T item in enumerable)
            {
                Add(item);
            }
        }
    }
}
