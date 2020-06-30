using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace PaZword.Api.Collections
{
    /// <summary>
    /// Represents a thread-safe dynamic data collection that provides notifications when items get added, removed, or when the whole list is refreshed.
    /// Actions that change the list, such as replace, add, remove, move, insert and clear are run on the UI thread.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public class ConcurrentObservableCollection<T> 
        : INotifyCollectionChanged,
        INotifyPropertyChanged,
        ICollection<T>,
        IEnumerable<T>,
        IEnumerable,
        IList<T>,
        IReadOnlyCollection<T>,
        IReadOnlyList<T>,
        ICollection,
        IList,
        IDisposable
    {
        private readonly CoreDispatcher _uiDispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
        private readonly ConcurrentQueue<PendingChange<T>> _pendingChanges = new ConcurrentQueue<PendingChange<T>>();
        private readonly ManualResetEvent _manualResetEvent = new ManualResetEvent(true);
        private readonly List<T> _items;

        private bool _processingChanges;
        private bool _isDisposed;

        public T this[int index]
        {
            get
            {
                WaitPendingChangesGetProcessedIfNotOnUIThread();
                return _items[index];
            }
            set => EnqueueChange(new PendingChange<T>(PendingChangeType.Replace, value, index));
        }

        object IList.this[int index]
        {
            get => this[index];
            set
            {
                AssertType(value, nameof(value));
                this[index] = (T)value;
            }
        }

        public int Count
        {
            get
            {
                WaitPendingChangesGetProcessedIfNotOnUIThread();
                return _items.Count;
            }
        }

        public bool IsReadOnly => ((IList)_items).IsReadOnly;

        public bool IsSynchronized => ((IList)_items).IsSynchronized;

        public object SyncRoot => ((IList)_items).SyncRoot;

        public bool IsFixedSize => ((IList)_items).IsFixedSize;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public ConcurrentObservableCollection()
        {
            _items = new List<T>();
        }

        public ConcurrentObservableCollection(IEnumerable<T> collection)
        {
            _items = new List<T>(collection);
        }

        ~ConcurrentObservableCollection()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                _manualResetEvent.Dispose();
            }

            _isDisposed = true;
        }

        public void Add(T item)
        {
            AssertNotReadOnly();
            EnqueueChange(new PendingChange<T>(PendingChangeType.Add, item));
        }

        public int Add(object value)
        {
            AssertType(value, nameof(value));
            int count = Count;
            Add((T)value);
            return count + 1;
        }

        public void Move(int oldIndex, int newIndex)
        {
            AssertNotReadOnly();
            EnqueueChange(new PendingChange<T>(PendingChangeType.Move, oldIndex, newIndex));
        }

        public void Clear()
        {
            AssertNotReadOnly();
            EnqueueChange(new PendingChange<T>(PendingChangeType.Clear));
        }

        public bool Contains(T item)
        {
            WaitPendingChangesGetProcessedIfNotOnUIThread();
            return _items.Contains(item);
        }

        public bool Contains(object value)
        {
            AssertType(value, nameof(value));
            return Contains((T)value);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            WaitPendingChangesGetProcessedIfNotOnUIThread();
            _items.CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index)
        {
            WaitPendingChangesGetProcessedIfNotOnUIThread();
            ((ICollection)_items).CopyTo(array, index);
        }

        public int IndexOf(T item)
        {
            WaitPendingChangesGetProcessedIfNotOnUIThread();
            return _items.IndexOf(item);
        }

        public int IndexOf(object value)
        {
            if (!(value is T))
            {
                return -1;
            }
            return IndexOf((T)value);
        }

        public void Insert(int index, T item)
        {
            AssertNotReadOnly();
            EnqueueChange(new PendingChange<T>(PendingChangeType.Insert, item, index));
        }

        public void Insert(int index, object value)
        {
            AssertType(value, nameof(value));
            Insert(index, (T)value);
        }

        public bool Remove(T item)
        {
            AssertNotReadOnly();
            EnqueueChange(new PendingChange<T>(PendingChangeType.Remove, item));
            return true; // can be inaccurate.
        }

        public void RemoveAt(int index)
        {
            AssertNotReadOnly();
            EnqueueChange(new PendingChange<T>(PendingChangeType.RemoveAt, index));
        }

        public void Remove(object value)
        {
            AssertType(value, nameof(value));
            Remove((T)value);
        }

        public IEnumerator<T> GetEnumerator()
        {
            WaitPendingChangesGetProcessedIfNotOnUIThread();
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public void WaitPendingChangesGetProcessedIfNotOnUIThread()
        {
            if (!_uiDispatcher.HasThreadAccess)
            {
                _manualResetEvent.WaitOne();
            }
        }

        private void EnqueueChange(PendingChange<T> change)
        {
            lock (SyncRoot)
            {
                // With this lock, we never run this method in parallel.
                _pendingChanges.Enqueue(change);

                if (_uiDispatcher.HasThreadAccess)
                {
                    // We're already on the UI thread.
                    _manualResetEvent.Reset();
                    _processingChanges = true;
                    ProcessPendingChanges();
                }
                else
                {
                    if (!_processingChanges)
                    {
                        _manualResetEvent.Reset();
                        _processingChanges = true;
                        // Start and forget the task. ProcessPendingChanges will be executed on the UI thread.
                        _ = _uiDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ProcessPendingChanges());
                    }
                }
            }
        }

        private void ProcessPendingChanges()
        {
            AssertOnUIThread();

            while (_pendingChanges.TryDequeue(out PendingChange<T> pendingEvent))
            {
                switch (pendingEvent.Type)
                {
                    case PendingChangeType.Add:
                        AddInternal(pendingEvent.Item);
                        break;

                    case PendingChangeType.Move:
                        MoveInternal(pendingEvent.Index, pendingEvent.Index2);
                        break;

                    case PendingChangeType.Remove:
                        RemoveInternal(pendingEvent.Item);
                        break;

                    case PendingChangeType.Clear:
                        ClearInternal();
                        break;

                    case PendingChangeType.Insert:
                        InsertItem(pendingEvent.Index, pendingEvent.Item);
                        break;

                    case PendingChangeType.RemoveAt:
                        RemoveInternalAt(pendingEvent.Index);
                        break;

                    case PendingChangeType.Replace:
                        ReplaceItem(pendingEvent.Index, pendingEvent.Item);
                        break;
                }
            }

            lock (SyncRoot)
            {
                _manualResetEvent.Set();
                _processingChanges = false;
            }
        }

        private void AddInternal(T item)
        {
            var index = _items.Count;
            _items.Add(item);

            RaiseCountPropertyChanged();
            RaiseIndexerPropertyChanged();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        private void MoveInternal(int oldIndex, int newIndex)
        {
            var item = _items[oldIndex];
            _items.RemoveAt(oldIndex);
            _items.Insert(newIndex, item);

            RaiseIndexerPropertyChanged();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
        }

        private void RemoveInternalAt(int index)
        {
            var item = _items[index];
            _items.RemoveAt(index);

            RaiseCountPropertyChanged();
            RaiseIndexerPropertyChanged();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        private void ClearInternal()
        {
            _items.Clear();

            RaiseCountPropertyChanged();
            RaiseIndexerPropertyChanged();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void InsertItem(int index, T item)
        {
            _items.Insert(index, item);

            RaiseCountPropertyChanged();
            RaiseIndexerPropertyChanged();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        private bool RemoveInternal(T item)
        {
            var index = _items.IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            RemoveInternalAt(index);

            return true;
        }

        private void ReplaceItem(int index, T item)
        {
            var oldItem = _items[index];
            _items[index] = item;

            RaiseIndexerPropertyChanged();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index));
        }

        private static void AssertType(object value, string valueName)
        {
            if (value is null || value is T)
            {
                return;
            }

            throw new ArgumentException($"Value must be of type '{typeof(T).FullName}'.", valueName);
        }

        private void AssertOnUIThread()
        {
            if (!_uiDispatcher.HasThreadAccess)
            {
                throw new Exception("This method should be called on the UI thread.");
            }
        }

        private void AssertNotReadOnly()
        {
            if (IsReadOnly)
            {
                throw new Exception("The collection is read-only.");
            }
        }

        private void RaiseCountPropertyChanged()
            => RaisePropertyChanged(nameof(Count));

        private void RaiseIndexerPropertyChanged()
            => RaisePropertyChanged("Item[]");

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
