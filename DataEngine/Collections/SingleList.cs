using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataEngine.Collections
{
    struct SingleList<T> where T : Entity
    {
        T Head; //initially it is null

        public T First
        {
            get
            {
                return Head as T;
            }
        }

        static public void AddAfter(T prev, T item)
        {
            item.Next = prev.Next;

            prev.Next = item;
        }

        public void AddFirst(T item)
        {
            item.Next = Head;

            Head = item;
        }

        static public void RemoveAfter(T prev)
        {
            prev.Next = prev.Next.Next;
        }

        /// <summary>
        /// This method should be called only if the first element exists.
        /// </summary>
        public void RemoveFirst()
        {
            Head = Head.Next as T;
        }

        public int Count
        {
            get
            {
                int count = 0;
                
                for (T cur = Head; cur != null; cur = cur.Next as T)
                {
                    ++count;
                }

                return count;
            }
        }

        public void Clear()
        {
            Head = null;
        }
    }
}
