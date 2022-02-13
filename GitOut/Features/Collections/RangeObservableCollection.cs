using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace GitOut.Features.Collections
{
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        private bool suppressNotification;

        public void AddRange(IEnumerable<T> list)
        {
            _ = list ?? throw new ArgumentNullException(nameof(list));

            suppressNotification = true;
            foreach (T item in list)
            {
                Add(item);
            }
            suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!suppressNotification)
            {
                base.OnCollectionChanged(e);
            }
        }
    }
}
