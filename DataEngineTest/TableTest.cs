using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataEngine;
using System.Collections.Generic;

namespace DataEngineTest
{
    [TestClass]
    public class TableTest
    {
        List<TestEntity> sampleList;

        Table<TestEntity> testTable;

        public TableTest()
        {
            //creation of two identical collections
            
            testTable = new Table<TestEntity>();

            testTable.CollectionChanged += testTable_CollectionChanged;

            sampleList = new List<TestEntity>()
            {
                new TestEntity(1),
                new TestEntity(2),
                new TestEntity(3),
                new TestEntity(4),
                new TestEntity(5),
                new TestEntity(6),
                new TestEntity(7)
            };

            foreach (TestEntity item in sampleList)
            {
                testTable.Add(item);
            }
        }

        void Compare()
        {
            Assert.AreEqual(testTable.Count, sampleList.Count);

            //for (int i = 0; i < sampleList.Count; ++i)
            //{
            //    //Assert.AreEqual(testTable[i].Id, sampleList[i].Id);

            //    Assert.AreEqual(testTable[i], sampleList[i], 
            //        String.Format("{0}-th elements of the collections differ.", i));
            //}

            Assert.IsTrue(cachedCount >= 0, "Something is wrong with INotifyCollectionChanged.");
            
            Assert.AreEqual(cachedCount, sampleList.Count, "Something is wrong with INotifyCollectionChanged.");
        }
        
        [TestMethod]
        public void AddRemoveTest()
        {
            Compare();

            {
                TestEntity item = sampleList[3];

                testTable.Remove(item);
                sampleList.Remove(item);

                Compare();

                item = sampleList[3];

                testTable.Remove(item);
                sampleList.Remove(item);

                Compare();
            }

            {
                TestEntity item = new TestEntity(8);

                testTable.Add(item);
                sampleList.Add(item);

                Compare();
            }

            {
                TestEntity existing_item = new TestEntity(3);
                TestEntity item = new TestEntity(15);

                testTable.Add(item);
                testTable.Add(existing_item);
                sampleList.Add(item);

                Compare();
            }

            {
                testTable.Clear();
                sampleList.Clear();

                Compare();
            }
        }

        int cachedCount = 0;

        void UpdateCachedCount(int diff)
        {
            cachedCount += diff;
        }
        
        void testTable_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    {
                        UpdateCachedCount(e.NewItems.Count);

                        break;
                    }
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    {
                        UpdateCachedCount(-e.OldItems.Count);

                        break;
                    }

                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    {
                        UpdateCachedCount(e.NewItems.Count);

                        UpdateCachedCount(-e.OldItems.Count);

                        break;
                    }
                
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    {
                        break;
                    }

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    {
                        cachedCount = testTable.Count;

                        break;
                    }
            }
        }
    }
}
