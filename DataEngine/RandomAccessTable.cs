using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Diagnostics;
using DataEngine.Collections;

namespace DataEngine
{
    //Although this is fine for "normal" properties, unfortunately, changes to collections 
    //still need to be marshaled back onto the UI thread using the Dispatcher. You cannot 
    //add or remove items from a data bound collection on any thread other than the main UI thread.

    public class RandomAccessTable<T> : NotificationObject, IList<T>, System.Collections.IList, INotifyCollectionChanged
        where T : Entity
    {
        #region IList<T> implementation

        //ListCollectionView calls this method after Add to
        //determine the index of added item
        public int IndexOf(T item)
        {
            return rbTree.IndexOfKey(item);
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException("Table.Insert(...) is not supported.");
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException("Table.RemoveAt(...) is not supported.");
        }

        public T this[int index]
        {
            get
            {
                return rbTree.KeyByIndex(index);
            }
            set
            {
                throw new NotSupportedException("Table[int index].set is not supported.");
            }
        }

        public void Add(T item)
        {
            InternalAdd(item);
        }

        public void Clear()
        {
            rbTree.Clear();

            RaiseCollectionChanged(
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            //ListView can pass null here
            if (item == null)
            {
                return false;
            }

            return rbTree.Find(Object2T(item)) != null;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            rbTree.Collection.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return rbTree.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(T item)
        {
            int index = rbTree.IndexOfKey(item);

            if (index != -1)
            {
                if (!rbTree.Remove(item))
                {
                    throw new InvalidOperationException(String.Format(
                        "Item {0} is not removed from Table for some reason.",
                        item));
                }

                RaiseCollectionChanged(
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        item,
                        index));

                return true;
            }

            return false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return rbTree.Collection.GetEnumerator();
        }

        #endregion

        #region IList implementation

        int System.Collections.IList.Add(object value)
        {
            return InternalAdd(Object2T(value));
        }

        void System.Collections.IList.Clear()
        {
            this.Clear();
        }

        bool System.Collections.IList.Contains(object value)
        {
            return this.Contains(value as T);
        }

        int System.Collections.IList.IndexOf(object value)
        {
            T t = value as T;

            if (t == null)
            {
                return -1;
            }

            return this.IndexOf(t);
        }

        void System.Collections.IList.Insert(int index, object value)
        {
            this.Insert(index, Object2T(value));
        }

        bool System.Collections.IList.IsFixedSize
        {
            get { return false; }
        }

        bool System.Collections.IList.IsReadOnly
        {
            get { return IsReadOnly; }
        }

        void System.Collections.IList.Remove(object value)
        {
            this.Remove(Object2T(value));
        }

        void System.Collections.IList.RemoveAt(int index)
        {
            this.RemoveAt(index);
        }

        object System.Collections.IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = Object2T(value);
            }
        }

        void System.Collections.ICollection.CopyTo(Array array, int index)
        {
            int i = index;

            foreach (T item in rbTree.Collection)
            {
                array.SetValue(item, i++);
            }
        }

        int System.Collections.ICollection.Count
        {
            get { return this.Count; }
        }

        //Default implementations of collections in the System.Collections.Generic 
        //namespace are not synchronized.
        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        object System.Collections.ICollection.SyncRoot
        {
            get { return rbTree; }
        }

        /// <summary>
        /// Returns an object that enumerates the items in this view.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region INotifyCollectionChanged implementation

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }

            if (e.Action != NotifyCollectionChangedAction.Move && e.Action != NotifyCollectionChangedAction.Replace)
            {
                RaisePropertyChanged(() => Count);
            }
        }

        #endregion

        #region Public members

        public T FindById(long id)
        {
            ITreeNode<T> node = rbTree.FindById(id);

            if (node != null)
            {
                return node.Key;
            }
            
            return null;
        }

        #endregion

        #region Private members

        T Object2T(object value)
        {
            T t = value as T;

            if (t == null)
            {
                throw new ArgumentException(
                    String.Format("The value '{0}' is not of type '{1}' and cannot be used in this generic collection.",
                    value, typeof(T).Name));
            }

            return t;
        }

        int InternalAdd(T item)
        {
            if (rbTree.Add(item) != null)
            {
                int index = rbTree.IndexOfKey(item);

                RaiseCollectionChanged(
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add,
                        item,
                        index));

                return index;
            }

            return -1;
        }

        //A tree structure could be constructed to access items by index or report the 
        //index of an existing item in lgN time, requiring very slight extra time for 
        //inserts and deletes. One way to do this would be to keep track of how many items 
        //are in the left and right branches of each node (on an insert or delete, change 
        //the node count of parent nodes when changing the count of the children).

        EntityTree<T> rbTree = new EntityTree<T>();

        #endregion
    }
}
