using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataEngine.Collections;

namespace DataEngineTest
{
    [TestClass]
    public class CollectionTest
    {
        [TestMethod]
        public void SingleListTest()
        {
            SingleList<TestEntity> list = new SingleList<TestEntity>();

            Assert.AreEqual(0, list.Count);

            var link1 = new TestEntity(1);

            list.AddFirst(link1);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(link1, list.First);

            var link2 = new TestEntity(2);

            SingleList<TestEntity>.AddAfter(link1, link2);

            Assert.AreEqual(2, list.Count);

            var link3 = new TestEntity(2);

            SingleList<TestEntity>.AddAfter(link2, link3);

            Assert.AreEqual(3, list.Count);

            list.RemoveFirst();

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(link2, list.First);

            SingleList<TestEntity>.RemoveAfter(link2);

            Assert.AreEqual(1, list.Count);

            list.RemoveFirst();

            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void HashTableTest()
        {
            HashTable<TestEntity> intHash = new HashTable<TestEntity>(3);

            Assert.AreEqual(null, intHash.Replace(new TestEntity(1)));
            Assert.AreEqual(null, intHash.Replace(new TestEntity(2)));
            Assert.AreEqual(null, intHash.Replace(new TestEntity(3)));

            Assert.AreEqual(intHash.Count, 3);

            Assert.AreEqual(new TestEntity(1), intHash.Replace(new TestEntity(1)));
            Assert.AreEqual(new TestEntity(2), intHash.Replace(new TestEntity(2)));
            Assert.AreEqual(new TestEntity(3), intHash.Replace(new TestEntity(3)));
            Assert.AreEqual(null, intHash.Replace(new TestEntity(4)));
            Assert.AreEqual(null, intHash.Replace(new TestEntity(5)));

            Assert.AreEqual(intHash.Count, 5);

            Assert.AreEqual(true, intHash.Remove(new TestEntity(1)));
            Assert.AreEqual(true, intHash.Remove(new TestEntity(3)));

            Assert.AreEqual(intHash.Count, 3);

            Assert.AreEqual(null, intHash.Replace(new TestEntity(1)));
            Assert.AreEqual(new TestEntity(4), intHash.Replace(new TestEntity(4)));

            Assert.AreEqual(intHash.Count, 4);

            Assert.AreEqual(true, intHash.Contains(new TestEntity(1)));
            Assert.AreEqual(true, intHash.Contains(new TestEntity(2)));
            Assert.AreEqual(false, intHash.Contains(new TestEntity(3)));

            Assert.AreEqual(intHash.Count, 4);

            intHash.Clear();

            Assert.AreEqual(intHash.Count, 0);

            Assert.AreEqual(null, intHash.Replace(new TestEntity(1)));
            Assert.AreEqual(null, intHash.Replace(new TestEntity(2)));
            Assert.AreEqual(null, intHash.Replace(new TestEntity(3)));

            Assert.AreEqual(intHash.Count, 3);
        }


        /*
        const int PeformanceTestSize = 1000000 * 10;

        [TestMethod]
        public void HashTableSystemPeformanceTest()
        {
            HashSet<TestEntity> intHash = new HashSet<TestEntity>();

            for (int i = 0; i < PeformanceTestSize; ++i)
            {
                intHash.Add(new TestEntity(i));
            }
        }

        [TestMethod]
        public void HashTablePeformanceTest()
        {
            HashTable<TestEntity> intHash = new HashTable<TestEntity>(PeformanceTestSize);

            for (int i = 0; i < PeformanceTestSize; ++i)
            {
                intHash.Replace(new TestEntity(i));
            }
        }*/
    }
}
