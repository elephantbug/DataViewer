using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataEngine
{
    //Although this is fine for "normal" properties, unfortunately, changes to collections 
    //still need to be marshaled back onto the UI thread using the Dispatcher. You cannot 
    //add or remove items from a data bound collection on any thread other than the main UI thread.

    public class Table<T> : ICollection<T>/*, System.Collections.ICollection*/, INotifyCollectionChanged
        where T : Entity
    {
        //A tree structure could be constructed to access items by index or report the 
        //index of an existing item in lgN time, requiring very slight extra time for 
        //inserts and deletes. One way to do this would be to keep track of how many items 
        //are in the left and right branches of each node (on an insert or delete, change 
        //the node count of parent nodes when changing the count of the children).

        Collections.HashTable<T> theHash = new Collections.HashTable<T>();

        #region ICollection<T> implementation

        public void Add(T item)
        {
            if (theHash.Replace(item) == null)
            {
                OnNotifyCollectionChanged(this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add,
                        item, -1));
            }
        }

        public bool Remove(T item)
        {
            if (theHash.Remove(item))
            {
                //standard CollectionView does not allow index == -1 here (throws System.InvalidOperationException)
                //it requires the index of the removing element
                OnNotifyCollectionChanged(this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        item, -1));

                return true;
            }

            return false;
        }

        public bool Contains(T item)
        {
            return theHash.Contains(item);
        }

        public void Clear()
        {
            theHash.Clear();

            OnNotifyCollectionChanged(this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset));
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            theHash.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return theHash.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return theHash.GetEnumerator();
        }

        /// <summary>
        /// Returns an object that enumerates the items in this view.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected void OnNotifyCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(sender, e);
            }
        }

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

        public T FindById(long id)
        {
            return theHash.FindById(id);
        }
        
        public T Replace(T item)
        {
            T old = theHash.Replace(item);

            OnNotifyCollectionChanged(this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace,
                    old, -1));

            return old;
        }

        public T RemoveById(long id)
        {
            T item = theHash.RemoveById(id);

            if (item != null)
            {
                //standard CollectionView does not allow index == -1 here (throws System.InvalidOperationException)
                //it requires the index of the removing element
                OnNotifyCollectionChanged(this,
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        item, -1));
            }

            return item;
        }

        /*
        void System.Collections.ICollection.CopyTo(Array array, int index)
        {
            rowList.CopyTo(array, index);
        }

        int System.Collections.ICollection.Count
        {
            get { return this.Count; }
        }

        //Default implementations of collections in the System.Collections.Generic 
        //namespace are not synchronized.
        bool System.Collections.ICollection.IsSynchronized
        {
            get { return iList.IsSynchronized; }
        }

        object System.Collections.ICollection.SyncRoot
        {
            get { return iList.SyncRoot; }
        }
        */
    }
}
