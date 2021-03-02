using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitOut.Features.Collections
{
    public class SortedLazyAsyncCollection<T> : SortedObservableCollection<T>, ILazyAsyncEnumerable<T>
    {
        private readonly Func<IAsyncEnumerable<T>> factory;
        private bool isMaterialized = false;

        public SortedLazyAsyncCollection(
            Func<IAsyncEnumerable<T>> factory,
            Func<T, T, int> comparer
        ) : base(comparer) => this.factory = factory;

        public bool IsMaterialized => isMaterialized;

        public async ValueTask MaterializeAsync()
        {
            if (isMaterialized)
            {
                return;
            }
            isMaterialized = true;
            IAsyncEnumerable<T> enumerable = factory();
            await foreach (T item in enumerable)
            {
                Add(item);
            }
        }
    }
}
