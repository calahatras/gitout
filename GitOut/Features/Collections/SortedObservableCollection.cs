using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace GitOut.Features.Collections
{
    public class SortedObservableCollection<T> : ICollection<T>, IReadOnlyCollection<T>, INotifyCollectionChanged
    {
        private readonly Func<T, T, int> comparer;
        private readonly IList<T> backingCollection = new List<T>();

        public SortedObservableCollection(Func<T, T, int> comparer) => this.comparer = comparer;
        public SortedObservableCollection(IEnumerable<T> initialItems, Func<T, T, int> comparer) : this(comparer)
        {
            foreach (T item in initialItems)
            {
                int index = FindSortedIndex(item);
                backingCollection.Insert(index, item);
            }
        }

        public int Count => backingCollection.Count;

        public bool IsReadOnly => false;

        public T this[int index] => backingCollection[index];

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public void Add(T item)
        {
            int index = FindSortedIndex(item);
            Insert(index, item);
        }

        public void Clear()
        {
            backingCollection.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            int index = FindSortedIndex(item);
            return backingCollection.Count >= index && backingCollection[index]!.Equals(item);
        }
        public int IndexOf(T item) => FindSortedIndex(item);

        public bool Remove(T item)
        {
            int index = FindSortedIndex(item);
            if (backingCollection.Count < index || backingCollection[index] is not T t || !t.Equals(item))
            {
                return false;
            }
            RemoveAt(FindSortedIndex(item));
            return true;
        }

        public void RemoveAt(int index)
        {
            T item = backingCollection[index];
            backingCollection.RemoveAt(index);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        public void CopyTo(T[] array, int arrayIndex) => backingCollection.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() => backingCollection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private void Insert(int index, T item)
        {
            backingCollection.Insert(index, item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        private int FindSortedIndex(T item)
        {
            if (Count == 0)
            {
                return 0;
            }
            int start = 0;
            int middle = Count / 2;
            int end = Count;

            do
            {
                if (middle == start)
                {
                    return comparer(item, backingCollection[middle]) > 0 ? end : start;
                }
                if (middle == end)
                {
                    return end;
                }
                int slice = comparer(item, backingCollection[middle]);
                if (slice == 0)
                {
                    return middle;
                }

                if (slice > 0)
                {
                    start = middle;
                    middle = (int)Math.Ceiling(Math.FusedMultiplyAdd(end - start, .5, start));
                }
                else
                {
                    end = middle;
                    middle = (int)Math.Floor(Math.FusedMultiplyAdd(end - start, .5, start));
                }
            } while (true);
        }
    }
}
