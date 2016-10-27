using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataEngine.Collections
{
    class EntityTree<T> : RBOrderedTree<T> where T : Entity
    {
        ///<summary>
        ///Find key in the dictionary
        ///This operation is O(logN) operation
        ///</summary>
        public ITreeNode<T> FindById(long id) //T aKey
        {
            RBTreeNodeBase<T, RBOrderedNodeParam> x, y;
            long cmp;

            //walk down the tree
            x = Root;
            while (x != null)
            {
                //cmp = mComparer.Compare(aKey, x.mKey);
                cmp = id - x.mKey.Id;
                if (cmp < 0)
                    x = x.mLeft;
                else if (cmp > 0)
                    x = x.mRight;
                else
                {
                    if (!Unique)
                    {
                        y = x;
                        y = Predecessor(y);
                        while (y != null && id == y.mKey.Id) //&& mComparer.Compare(aKey, y.mKey) == 0
                        {
                            x = y;
                            y = Predecessor(y);
                        }
                    }
                    return x;
                }
            }
            
            return null;
        }
    }
}
