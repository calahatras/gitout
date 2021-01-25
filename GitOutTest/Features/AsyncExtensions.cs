using System.Collections.Generic;

namespace GitOut.Features
{
    public static class AsyncExtensions
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> enumerable)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            foreach (T item in enumerable)
            {
                yield return item;
            }
        }
    }
}
