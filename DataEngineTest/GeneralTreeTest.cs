using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using DataEngine.Collections;
using System.Diagnostics;

namespace DataEngineTest
{
    [TestClass]
    public class GeneralTreeTest
    {
        [TestMethod]
        public void SimpleTreeTest()
        {
            //Console.WriteLine("rbtree");

            Random r = new Random((int)(DateTime.Now.Ticks ^ 0xffff));
            RBTree<int> tree = new RBTree<int>();
            List<int> vals = new List<int>();
            List<int> removed = new List<int>();
            int i, j, k;

            //fill tree
            while (tree.Count != 1000)
            {
                i = r.Next(10000);
                if (tree.Find(i) != null)
                    continue;
                tree.Add(i);
                vals.Add(i);
            }

            //check presence of the values
            k = 0;
            foreach (int val in vals)
                if (tree.Find(val) == null)
                    k++;

            Assert.AreEqual(k, 0);
            //Console.WriteLine("{0}", k == 0 ? "ok" : "failed");


            //check order
            j = -1;
            foreach (RBTreeNode<int> node in tree)
                if (node.Key < j)
                    k++;
            Assert.AreEqual(k, 0);
            //Console.WriteLine("{0}", k == 0 ? "ok" : "failed");

            //check remove
            for (i = 0; i < vals.Count; i += 25)
            {
                tree.Remove(vals[i]);
                removed.Add(vals[i]);
                vals.RemoveAt(i);
            }

            k = 0;
            foreach (int val in vals)
                if (tree.Find(val) == null)
                    k++;
            foreach (int val in removed)
                if (tree.Find(val) != null)
                    k++;
            Assert.AreEqual(k, 0);
            //Console.WriteLine("{0}", k == 0 ? "ok" : "failed");

            //chaos test
            k = 0;
            for (j = 0; j < 50; j++)
            {
                tree.Clear();
                vals.Clear();
                removed.Clear();

                //fill tree
                while (tree.Count != 5000)
                {
                    i = r.Next(100000);
                    if (tree.Find(i) != null)
                        continue;
                    tree.Add(i);
                    vals.Add(i);
                }

                while (tree.Count != 100)
                {
                    i = r.Next(vals.Count);
                    tree.Remove(vals[i]);

                    removed.Add(vals[i]);
                    vals.RemoveAt(i);
                }

                foreach (int val in vals)
                    if (tree.Find(val) == null)
                        k++;
                foreach (int val in removed)
                    if (tree.Find(val) != null)
                        k++;
            }
            Assert.AreEqual(k, 0);
            //Console.WriteLine("{0}", k == 0 ? "ok" : "failed");
        }

        [TestMethod]
        public void MultiTreeTest()
        {
            //Console.WriteLine("rbmultitree");
            int[] counts = new int[1000];
            Random r = new Random((int)(DateTime.Now.Ticks ^ 0xffff));
            int i, j, k;
            RBMultiTree<int> tree = new RBMultiTree<int>();

            //fill tree
            for (i = 0; i < 1000; i++)
                counts[i] = 0;

            j = 0;
            for (i = 0; i < 2000; i++)
            {
                k = r.Next(1000);
                counts[k]++;
                if (counts[k] > j)
                    j = counts[k];
                tree.Add(k);
            }

            Debug.Print("max {0}", j);
            //Console.WriteLine("max {0}", j);

            k = 0;
            //verify filling
            for (i = 0; i < 1000; i++)
            {
                //calculate counts
                j = 0;

                RBTreeNode<int> node = tree.Find(i);
                while (node != null && node.Key == i)
                {
                    j++;
                    node = tree.Next(node);
                }

                if (j != counts[i])
                    k++;
            }
            Assert.AreEqual(k, 0);
            //Console.WriteLine("{0}", k == 0 ? "ok" : "failed");

            //check order
            k = 0;
            j = -1;
            foreach (RBTreeNode<int> node in tree)
                if (node.Key < j)
                    k++;
            Assert.AreEqual(k, 0);
            //Console.WriteLine("{0}", k == 0 ? "ok" : "failed");

            //remove
            k = 0;
            for (i = 0; i < 200; i++)
            {
                j = r.Next(1000);
                if (counts[j] == 0)
                {
                    if (tree.Remove(j))
                        k++;
                }
                else
                {
                    if (!tree.Remove(j))
                        k++;
                    counts[j]--;
                }
            }
            Assert.AreEqual(k, 0);
            //Console.WriteLine("{0}", k == 0 ? "ok" : "failed");

            k = 0;
            //verify filling
            for (i = 0; i < 1000; i++)
            {
                //calculate counts
                j = 0;

                RBTreeNode<int> node = tree.Find(i);
                while (node != null && node.Key == i)
                {
                    j++;
                    node = tree.Next(node);
                }

                if (j != counts[i])
                    k++;
            }
            Assert.AreEqual(k, 0);
            //Console.WriteLine("{0}", k == 0 ? "ok" : "failed");

            //check order
            k = 0;
            j = -1;
            foreach (RBTreeNode<int> node in tree)
                if (node.Key < j)
                    k++;
            Assert.AreEqual(k, 0);
            //Console.WriteLine("{0}", k == 0 ? "ok" : "failed");
        }

        [TestMethod]
        public void OrderedTreeTest()
        {
            Random r = new Random((int)(DateTime.Now.Ticks ^ 0xffff));
            RBOrderedTree<int> tree = new RBOrderedTree<int>();
            List<int> vals = new List<int>();
            int i, j, k;

            //fill tree
            while (tree.Count != 1000)
            {
                i = r.Next(10000);
                if (tree.Find(i) != null)
                    continue;
                tree.Add(i);
                vals.Add(i);
            }
            vals.Sort();

            //check ordering
            k = 0;
            for (i = 0; i < vals.Count; i++)
                if (tree.GetByOrder(i).Key != vals[i])
                    k++;

            Assert.AreEqual(0, k);
            //Console.WriteLine("{0}", k == 0 ? "ok" : "failed");

            //check get order operation
            k = 0;
            for (i = 0; i < tree.Count; i++)
            {
                if (tree.GetOrder(tree.GetByOrder(i)) != i)
                    k++;
            }
            Assert.AreEqual(0, k);
            //Console.WriteLine("{0}", k == 0 ? "ok" : "failed");

            //remove some items and re-check
            for (i = 0; i < 50; i++)
            {
                j = r.Next(vals.Count);
                tree.Remove(vals[j]);
                vals.RemoveAt(j);
            }

            //check ordering
            k = 0;
            for (i = 0; i < vals.Count; i++)
                if (tree.GetByOrder(i).Key != vals[i])
                    k++;
            Assert.AreEqual(0, k);
            //Console.WriteLine("{0}", k == 0 ? "ok" : "failed");

            //check get order operation
            k = 0;
            for (i = 0; i < tree.Count; i++)
            {
                if (tree.GetOrder(tree.GetByOrder(i)) != i)
                    k++;
            }
            Assert.AreEqual(0, k);
            //Console.WriteLine("{0}", k == 0 ? "ok" : "failed");
        }
    }
}
