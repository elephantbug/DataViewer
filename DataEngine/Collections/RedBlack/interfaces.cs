using System;
using System.Collections;
using System.Collections.Generic;

namespace DataEngine.Collections
{
    ///<summary>
    ///Interface to the node
    ///</summary>
    interface ITreeNode<T>
    {
        T Key
        {
            get;
        }
    }

    ///<summary>
    ///Interface to the tree
    ///</summary>
    interface ITree<T>
    {
        ///<summary>
        ///Get synchornization root
        ///</summary>
        object SyncRoot
        {
            get;
        }

        ///<summary>
        ///Add item
        ///</summary>
        ITreeNode<T> Add(T item);

        /// <summary>
        /// Adds the item contained in node.Key and reuses existing node
        /// </summary>
        void Add(ITreeNode<T> node);

        ///<summary>
        ///Add or get item
        ///</summary>
        ITreeNode<T> AddOrGet(T item);

        ///<summary>
        ///Find item
        ///</summary>
        ITreeNode<T> Find(T item);

        ///<summary>
        ///Delete item by key
        ///</summary>
        bool Remove(T item);
        
        ///<summary>
        ///Delete specific item 
        ///</summary>
        void Remove(ITreeNode<T> node);

        ///<summary>
        ///Clear the tree
        ///</summary>
        void Clear();

        /// <summary>
        /// Adapter used for iteration over the Tree
        /// </summary>
        ICollection<T> Collection { get; }
    }

    ///<summary>
    ///Interface to the tree
    ///</summary>
    interface ISortedTree<T> : ITree<T>
    {
        IComparer<T> Comparer { get; set; }

        ///<summary>
        ///Get number of nodes in the tree
        ///</summary>
        int Count
        {
            get;
        }

        ///<summary>
        ///Get first node
        ///</summary>
        ITreeNode<T> First();

        ///<summary>
        ///Get last node
        ///</summary>
        ITreeNode<T> Last();

        ///<summary>
        ///Get next node
        ///</summary>
        ITreeNode<T> Previous(ITreeNode<T> node);

        ///<summary>
        ///Get prior node
        ///</summary>
        ITreeNode<T> Next(ITreeNode<T> node);
    }

    ///<summary>
    ///Interface to the tree which supports direct access to the items
    ///</summary>
    ///<summary>
    ///Interface to the tree
    ///</summary>
    interface IOrderedTree<T> : ISortedTree<T>
    {
        ///<summary>
        ///Get item by order index
        ///</summary>
        ITreeNode<T> GetByOrder(int idx);

        ///<summary>
        ///Get index by item
        ///</summary>
        int GetOrder(ITreeNode<T> node);

        int IndexOfKey(T key);

        T KeyByIndex(int index);
    }
}
