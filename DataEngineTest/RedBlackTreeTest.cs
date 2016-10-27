using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using DataEngine.Collections;

namespace DataEngineTest
{
    [TestClass]
    public class RedBlackTreeTest
    {
        RBOrderedTree<string> Tree;

        const int InitialCount = 10;

        int lastId = 0;

        void AddNew()
        {
            ++lastId;

            Tree.Add(String.Format("{0:000}", lastId));
        }

        void Reverse()
        {
            Tree.Comparer = new DataEngine.ReverseComparer<string>();
        }

        #region Additional test attributes

        [TestInitialize()]
        public void TestInitialize()
        {
            Tree = new RBOrderedTree<string>();

            for (int i = 0; i < InitialCount; ++i)
            {
                AddNew();
            }
        }

        [TestCleanup()]
        public void TestCleanup()
        {
            Tree = null;
        }

        #endregion

        [TestMethod]
        public void IndexTest()
        {
            Assert.AreEqual(Tree.Count, InitialCount);

            RBOrderedTreeNode<string> node = Tree.Find("005");

            Assert.AreEqual("005", node.Key);

            node = Tree.GetByOrder(0);

            Assert.AreEqual("001", node.Key);

            node = Tree.GetByOrder(4);

            Assert.AreEqual("005", node.Key);

            node = Tree.GetByOrder(2);

            Assert.AreEqual("003", node.Key);

            node = Tree.GetByOrder(5);

            Assert.AreEqual("006", node.Key);

            node = Tree.GetByOrder(7);

            Assert.AreEqual("008", node.Key);
        }

        int IndexOfKey(string key)
        {
            return Tree.IndexOfKey(key);
            
            //RBOrderedTreeNode<string> node = Tree.Find(key);

            //if (node == null)
            //{
            //    return -1;
            //}

            //return Tree.GetOrder(node);
        }

        [TestMethod]
        public void ReverseTest()
        {
            Assert.AreEqual(InitialCount, Tree.Count);

            Reverse();

            Assert.AreEqual(InitialCount, Tree.Count);

            RBOrderedTreeNode<string> node = Tree.Find("005");

            Assert.AreEqual("005", node.Key);

            node = Tree.GetByOrder(0);

            Assert.AreEqual("010", node.Key);

            node = Tree.GetByOrder(4);

            Assert.AreEqual("006", node.Key);

            node = Tree.GetByOrder(9);

            Assert.AreEqual("001", node.Key);
        }

        [TestMethod]
        public void IndexOfKeyTest()
        {
            Assert.AreEqual(InitialCount, Tree.Count);

            int index = IndexOfKey("005");

            Assert.AreEqual(4, index);

            index = IndexOfKey("001");

            Assert.AreEqual(0, index);

            index = IndexOfKey("010");

            Assert.AreEqual(9, index);

            Reverse();

            Assert.AreEqual(0, IndexOfKey("010"));

            Assert.AreEqual(1, IndexOfKey("009"));

            Assert.AreEqual(2, IndexOfKey("008"));

            Assert.AreEqual(3, IndexOfKey("007"));

            Assert.AreEqual(4, IndexOfKey("006"));

            Assert.AreEqual(5, IndexOfKey("005"));

            Assert.AreEqual(6, IndexOfKey("004"));

            Assert.AreEqual(7, IndexOfKey("003"));

            Assert.AreEqual(8, IndexOfKey("002"));

            Assert.AreEqual(9, IndexOfKey("001"));

            Assert.AreEqual(-1, IndexOfKey("00A"));
        }

        [TestMethod]
        public void DeleteTest()
        {
            Assert.AreEqual(InitialCount, Tree.Count);

            Assert.AreEqual(3, IndexOfKey("004"));

            Tree.Remove("004");

            Assert.AreEqual(-1, IndexOfKey("004"));

            Assert.AreEqual(InitialCount - 1, Tree.Count);
        }

        [TestMethod]
        //[ExpectedException(typeof(IndexOutOfRangeException), "A negative index was inappropriately allowed.")]
        public void ZeroIndexTest()
        {
            Tree.Clear();

            RBOrderedTreeNode<string> node = Tree.GetByOrder(0);

            Assert.IsNull(node);

            Reverse();

            node = Tree.GetByOrder(0);

            Assert.IsNull(node);
        }

        [TestMethod]
        //[ExpectedException(typeof(IndexOutOfRangeException), "A negative index was inappropriately allowed.")]
        public void NegativeIndexTest()
        {
            RBOrderedTreeNode<string> node = Tree.GetByOrder(-1);

            Assert.IsNull(node, "A negative index was inappropriately allowed.");
            
            Reverse();

            node = Tree.GetByOrder(-1);

            Assert.IsNull(node, "A negative index was inappropriately allowed.");
        }

        [TestMethod]
        //[ExpectedException(typeof(IndexOutOfRangeException), "A index grater then size of the collection was inappropriately allowed.")]
        public void CountIndexTest()
        {
            RBOrderedTreeNode<string> node = Tree.GetByOrder(Tree.Count);

            Assert.IsNull(node, "A index grater then size of the collection was inappropriately allowed.");

            node = Tree.GetByOrder(Tree.Count - 1);

            Assert.AreEqual("010", node.Key);

            Reverse();

            node = Tree.GetByOrder(Tree.Count - 1);

            Assert.AreEqual("001", node.Key);
        }
    }
}
