using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace DataEngine.Collections
{
    class HashTable<T> : ICollection<T>, IEnumerable<T> where T : Entity
    {
        SingleList<T>[] Buckets;

        int elementCount = 0;

        public HashTable() : this(10000)
        {
        }
        
        public HashTable(int size)
        {
            Buckets = new SingleList<T>[size];
        }

        int TableSize
        {
            get
            {
                return Buckets.Length;
            }
        }

        int GetBucketIndex(T item)
        {
            return GetBucketIndex(item.Id);
        }

        int GetBucketIndex(long id)
        {
            int position = id.GetHashCode() % TableSize;

            return Math.Abs(position);
        }

        //SingleLink<T> GetBucket(int index)
        //{
        //    return Buckets[index].First;
        //}

        T FindLink(T first, long id)
        {
            T found_link = null;

            for (T cur = first; cur != null; cur = cur.Next as T)
            {
                if (cur.Id.Equals(id))
                {
                    found_link = cur;
                }
            }

            return found_link;
        }

        /// <summary>
        /// Adds or replaces the element.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Returns old element if exists.</returns>
        //Probably, we do not need to replace the entities in the tables.
        public T Replace(T item)
        {
            return Replace(item.Id, item);
        }

        public T Replace(long id, T item)
        {
            int index = GetBucketIndex(id);

            T first = Buckets[index].First;

            T prev = null;

            T removed = null;

            for (T cur = first; cur != null; cur = cur.Next as T)
            {
                if (cur.Id.Equals(id))
                {
                    if (prev != null)
                    {
                        removed = prev.Next as T;
                        
                        SingleList<T>.RemoveAfter(prev);
                    }
                    else
                    {
                        Debug.Assert(Buckets[index].First.Id.Equals(id));

                        removed = Buckets[index].First;

                        Buckets[index].RemoveFirst();
                    }

                    break;
                }

                prev = cur;
            }

            if (item != null)
            {
                Buckets[index].AddFirst(item);
                
                ++elementCount;
            }

            if (removed != null)
            {
                --elementCount;
            }

            return removed;
        }

        public T Find(T item)
        {
            return FindById(item.Id);
        }
        
        public T FindById(long id)
        {
            int index = GetBucketIndex(id);

            T first = Buckets[index].First;

            return FindLink(first, id);
        }

        /// <summary>
        /// it is a bad practice to iterate over a hash table of cause
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Buckets.Length; ++i)
            {
                T first = Buckets[i].First;

                for (T cur = first; cur != null; cur = cur.Next as T)
                {
                    yield return cur;
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #region ICollection<T> implementation

        public void Add(T item)
        {
            Replace(item);
        }

        public bool Remove(T item)
        {
            return RemoveById(item.Id) != null;
        }

        public T RemoveById(long id)
        {
            return Replace(id, null);
        }

        public bool Contains(T item)
        {
            T found = Find(item);

            return found != null;
        }

        public void Clear()
        {
            elementCount = 0;

            //Buckets are value types, so they should be iterated using index
            for (int i = 0; i < Buckets.Length; ++i)
            {
                Buckets[i].Clear();
            }

            elementCount = 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            int i = 0;
            
            foreach (SingleList<T> bucket in Buckets)
            {
                for (T cur = bucket.First; cur != null; cur = cur.Next as T)
                {
                    array[arrayIndex + i] = cur;

                    ++i;
                }
            }
        }

        public int Count
        {
            get { return elementCount; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }
        
        #endregion ICollection<T> implementation
    }
}
