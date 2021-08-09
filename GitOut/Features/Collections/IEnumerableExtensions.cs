using System;
using System.Collections.Generic;
using System.Linq;

namespace GitOut.Features.Collections
{
    public static class IEnumerableExtensions
    {
        public static int FindIndex<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            int index = 0;
            foreach (T item in collection)
            {
                if (predicate(item))
                {
                    return index;
                }
                ++index;
            }
            return -1;
        }

        public static int FindIndex<T>(this IEnumerable<T> collection, int startFrom, Func<T, bool> predicate)
        {
            int index = startFrom;
            foreach (T item in collection.Skip(startFrom))
            {
                if (predicate(item))
                {
                    return index;
                }
                ++index;
            }
            return -1;
        }
    }
}
