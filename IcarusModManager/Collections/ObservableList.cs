// Copyright 2022 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using IcarusModManager.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace IcarusModManager.Collections
{
    /// <summary>
    /// An observable generic list
    /// </summary>
    /// <typeparam name="T">The type of item stored in the list</typeparam>
    public class ObservableList<T> : IList<T>, ICollection<T>, IEnumerable<T>, IObservableCollection<T>, IList, ICollection, IEnumerable, INotifyCollectionChanged
    {
        /// <summary>
        /// Storage of list items
        /// </summary>
        private readonly List<T> mList;

        /// <summary>
        /// Gets the number of items in the list
        /// </summary>
        public int Count
        {
            get { return mList.Count; }
        }

        /// <summary>
        /// Fired when the contents of the collection have changed
        /// </summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <summary>
        /// Creates a new instance of the ObservableList&lt;T&gt; class
        /// </summary>
        public ObservableList()
        {
            mList = new List<T>();
        }

        /// <summary>
        /// Creates a new instance of the ObservableList&lt;T&gt; class
        /// </summary>
        /// <param name="capacity">The number of item spaces to initially allocate</param>
        public ObservableList(int capacity)
        {
            mList = new List<T>(capacity);
        }

        /// <summary>
        /// Creates a new instance of the ObservableList&lt;T&gt; class
        /// </summary>
        /// <param name="collection">A collection of items to populate the list with</param>
        public ObservableList(IEnumerable<T> collection)
        {
            mList = new List<T>(collection);
        }

        /// <summary>
        /// Adds an item to the end of the list
        /// </summary>
        /// <param name="item">The item to add</param>
        public void Add(T item)
        {
            int index = mList.Count;
            mList.Add(item);
            NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        /// <summary>
        /// Adds a collection of items to the end of the list
        /// </summary>
        /// <param name="collection">The items to add</param>
        public void AddRange(IEnumerable<T> collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");

            int index = mList.Count;
            mList.AddRange(collection);

            // CollectionView implementations do not support range changes, and will throw an exception if we fire one while it is listening.
            // So, we have to fire a reset instead, unless only one item was added...
            if (collection.CountEqualsOne())
            {
                NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, mList[index], index));
            }
            else
            {
                NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// Inserts an item into the list
        /// </summary>
        /// <param name="index">The index at which to insert the item</param>
        /// <param name="item">The item to insert</param>
        public void Insert(int index, T item)
        {
            if (index < 0 || index > mList.Count) throw new ArgumentOutOfRangeException("index");

            mList.Insert(index, item);
            NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        /// <summary>
        /// Inserts a collection of items into the list
        /// </summary>
        /// <param name="index">>The index at which to insert the items</param>
        /// <param name="collection">The items to insert</param>
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (index < 0 || index > mList.Count) throw new ArgumentOutOfRangeException("index");
            if (collection == null) throw new ArgumentNullException("collection");

            mList.InsertRange(index, collection);

            // CollectionView implementations do not support range changes, and will throw an exception if we fire one while it is listening.
            // So, we have to fire a reset instead, unless only one item was added...
            if (collection.CountEqualsOne())
            {
                NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, mList[index], index));
            }
            else
            {
                NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// Moves an item in the list from one index to another
        /// </summary>
        /// <param name="sourceIndex">The index of the item to move</param>
        /// <param name="destinationIndex">The index at which to place the item</param>
        public void Move(int sourceIndex, int destinationIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= mList.Count) throw new ArgumentOutOfRangeException("sourceIndex");
            if (destinationIndex < 0 || destinationIndex >= mList.Count) throw new ArgumentOutOfRangeException("destinationIndex");

            T item = mList[sourceIndex];
            mList.RemoveAt(sourceIndex);
            mList.Insert(destinationIndex, item);
            NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, destinationIndex, sourceIndex));
        }

        /// <summary>
        /// Removes the item at the specified index from the list
        /// </summary>
        /// <param name="index">The index of the item to remove</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= mList.Count) throw new ArgumentOutOfRangeException("index");

            T item = mList[index];
            mList.RemoveAt(index);
            NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        /// <summary>
        /// Removes an item from the list
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns>True if the item was found and removed, else false</returns>
        public bool Remove(T item)
        {
            int index = mList.IndexOf(item);
            if (index >= 0)
            {
                mList.RemoveAt(index);
                NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes all items from the list
        /// </summary>
        public void Clear()
        {
            mList.Clear();
            NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Gets or sets the item at the specified index in the list
        /// </summary>
        /// <param name="index">The index of the item</param>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= mList.Count) throw new IndexOutOfRangeException("index");
                return mList[index];
            }
            set
            {
                if (index < 0 || index >= mList.Count) throw new IndexOutOfRangeException("index");
                T oldValue = mList[index];
                mList[index] = value;
                NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldValue, index));
            }
        }

        /// <summary>
        /// Searches the list for an item and returns its index
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <returns>Thhe index of the item in the list, or -1 if it was not found</returns>
        public int IndexOf(T item)
        {
            return mList.IndexOf(item);
        }

        /// <summary>
        /// Searches the list for an item and returns its index
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <param name="index">The index within the list from which to start searching</param>
        /// <returns>Thhe index of the item in the list, or -1 if it was not found</returns>
        public int IndexOf(T item, int index)
        {
            return mList.IndexOf(item, index);
        }

        /// <summary>
        /// Searches the list for an item and returns its index
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <param name="index">The index within the list from which to start searching</param>
        /// <param name="count">The number of items to search</param>
        /// <returns>Thhe index of the item in the list, or -1 if it was not found</returns>
        public int IndexOf(T item, int index, int count)
        {
            return mList.IndexOf(item, index, count);
        }

        /// <summary>
        /// Returns whether the list contains the specified item
        /// </summary>
        /// <param name="item">The item to search for</param>
        public bool Contains(T item)
        {
            return mList.Contains(item);
        }

        /// <summary>
        /// Returns whether the list contains the specified item
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <param name="comparer">Comparer to use when comparing items</param>
        public bool Contains(T item, IEqualityComparer<T> comparer)
        {
            return mList.Contains(item, comparer);
        }

        /// <summary>
        /// Searches the sorted list for an item and returns its index
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <returns>The index of the item, or the bitwise complement of the insertion index if the item was not found</returns>
        public int BinarySearch(T item)
        {
            return mList.BinarySearch(item);
        }

        /// <summary>
        /// Searches the sorted list for an item and returns its index
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <param name="comparer">Comparer to use when comparing items</param>
        /// <returns>The index of the item, or the bitwise complement of the insertion index if the item was not found</returns>
        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return mList.BinarySearch(item, comparer);
        }

        /// <summary>
        /// Searches the sorted list for an item and returns its index
        /// </summary>
        /// <param name="index">The index within the list from which to start searching</param>
        /// <param name="count">The number of items to search</param>
        /// <param name="item">The item to search for</param>
        /// <param name="comparer">Comparer to use when comparing items</param>
        /// <returns>The index of the item, or the bitwise complement of the insertion index if the item was not found</returns>
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            return mList.BinarySearch(index, count, item, comparer);
        }

        /// <summary>
        /// Performs an action for each item in the list
        /// </summary>
        /// <param name="action">The action to perform</param>
        public void ForEach(Action<T> action)
        {
            mList.ForEach(action);
        }

        /// <summary>
        /// Creates a shallow copy of a range of elements in the list
        /// </summary>
        /// <param name="index">The index at which the range starts</param>
        /// <param name="count">The number of items in the range</param>
        public List<T> GetRange(int index, int count)
        {
            return mList.GetRange(index, count);
        }

        /// <summary>
        /// Sorts the list using the default comparer
        /// </summary>
        public void Sort()
        {
            mList.Sort();
            // TODO: Is there some easy way to detect if anything changed after calling sort?
            NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Sorts the list using the specified comparison
        /// </summary>
        /// <param name="comparison">The comparison to use when comparing items</param>
        public void Sort(Comparison<T> comparison)
        {
            mList.Sort(comparison);
            // TODO: Is there some easy way to detect if anything changed after calling sort?
            NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Sorts the list using the specified comparer
        /// </summary>
        /// <param name="comparer">The comparer to use when comparing items</param>
        public void Sort(IComparer<T> comparer)
        {
            mList.Sort(comparer);
            // TODO: Is there some easy way to detect if anything changed after calling sort?
            NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Returns an enumerator that can be used to enumerate the contents of the list
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return mList.GetEnumerator();
        }

        /// <summary>
        /// Copies the entire list to the specified array
        /// </summary>
        /// <param name="array">The array to copy to</param>
        /// <param name="arrayIndex">The index in the target array in which to start copying</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            mList.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Helper method to fire collection changed notifications
        /// </summary>
        private void NotifyCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var handler = CollectionChanged;
            if (handler != null)
            {
                handler.Invoke(this, e);
            }
        }

        #region Explicit interface implementations
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mList.GetEnumerator();
        }

        int IList.Add(object? value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Add((T)value);
            return mList.Count - 1;
        }

        void IList.Insert(int index, object? value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Insert(index, (T)value);
        }

        void IList.Remove(object? value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Remove((T)value);
        }

        void IList.RemoveAt(int index)
        {
            RemoveAt(index);
        }

        void IList.Clear()
        {
            Clear();
        }

        object? IList.this[int index]
        {
            get { return this[index]; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                this[index] = (T)value;
            }
        }

        bool IList.Contains(object? value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return Contains((T)value);
        }

        int IList.IndexOf(object? value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return IndexOf((T)value);
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((IList)mList).CopyTo(array, index);
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return false; }
        }

        int ICollection.Count
        {
            get { return Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return ((ICollection)mList).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return ((ICollection)mList).SyncRoot; }
        }
        #endregion
    }
}
