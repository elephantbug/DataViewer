using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataEngine
{
    public class ReverseComparer<T> : IComparer<T>  
    {
        readonly IComparer<T> innerComparer;

        public ReverseComparer() : this(Comparer<T>.Default)
        {
        }
        
        public ReverseComparer(IComparer<T> inner)
        {
            innerComparer = inner;
        }

        public int Compare(T left, T right)
        {
            return innerComparer.Compare(right, left);
        }
    }

}
