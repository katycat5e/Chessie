using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chessie.Model
{
    public class ObservableStack<T> : Stack<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public new virtual void Clear()
        {
            base.Clear();
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset));
        }

        public new virtual T Pop()
        {
            var item = base.Pop();
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Remove, item));
            return item;
        }

        public new virtual void Push(T item)
        {
            base.Push(item);
            CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Add, item));
        }
    }
}
