using System.Collections;
using System.Collections.Specialized;

namespace JHLib.WPFUtil.UI
{
    public class ObservableReadOnlyList<T>(IReadOnlyList<T> list) : IReadOnlyList<T>, INotifyCollectionChanged
    {
        private readonly IReadOnlyList<T> _list = list;
        public T this[int index] => _list[index];
        public int Count => _list.Count;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public void Refresh() => CollectionChanged?.Invoke(this, new(NotifyCollectionChangedAction.Reset));

        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}