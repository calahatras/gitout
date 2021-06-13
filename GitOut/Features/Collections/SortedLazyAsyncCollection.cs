using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitOut.Features.IO;

namespace GitOut.Features.Collections
{
    public class SortedLazyAsyncCollection<T> : SortedObservableCollection<T>, ILazyAsyncEnumerable<T>
    {
        private readonly Func<RelativeDirectoryPath, IAsyncEnumerable<T>> factory;
        private bool isMaterialized;

        public SortedLazyAsyncCollection(
            Func<RelativeDirectoryPath, IAsyncEnumerable<T>> factory,
            Func<T, T, int> comparer
        ) : base(comparer) => this.factory = factory;

        public bool IsMaterialized => isMaterialized;

        public async ValueTask MaterializeAsync(RelativeDirectoryPath path)
        {
            if (isMaterialized)
            {
                return;
            }
            isMaterialized = true;
            IAsyncEnumerable<T> enumerable = factory(path);
            await foreach (T item in enumerable)
            {
                Add(item);
            }
        }
    }
}
