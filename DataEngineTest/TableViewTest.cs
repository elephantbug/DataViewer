using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;
using DataEngine;

namespace DataEngineTest
{
    /// <summary>
    /// Summary description for TableViewTest
    /// </summary>
    [TestClass]
    public class TableViewTest
    {
        Table<TestEntity> testTable;

        TableView<TestEntity> testView;
        
        public TableViewTest()
        {
            LoggerFactory.Writer = new FakeLogWriter();
        }

        const int InitialCount = 10;

        int lastId = 0;

        void AddNew(long id)
        {
            testTable.Add(
                new TestEntity(id)
                {
                    Name = String.Format("target-{0}", id)
                });
        }

        void AddNew()
        {
            ++lastId;

            AddNew(lastId);
        }
        
        #region Additional test attributes

        [TestInitialize()]
        public void TestInitialize()
        {
            testTable = new Table<TestEntity>();

            for (int i = 0; i < InitialCount; ++i)
            {
                AddNew();
            }

            testView = new TestView(testTable);
        }
        
        [TestCleanup()]
        public void TestCleanup()
        {
            testTable = null;

            testView = null;
        }
        
        #endregion

        public static void CheckInstanceCount()
        {
            Assert.AreEqual(TestView.InstanceCount, 0, "Looks like there are some memory leaks, probably due to event subscriptions");
        }

        [TestMethod]
        public void WeakEventsTest()
        {
            testView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

            testView = null;
            
            GC.Collect();

            GC.WaitForPendingFinalizers();

            CheckInstanceCount();
        }

        [TestMethod]
        public void SyncTest()
        {
            Assert.AreEqual(InitialCount, testView.Count);

            testTable.RemoveById(1);
            
            Assert.AreEqual(InitialCount - 1, testView.Count);

            testTable.RemoveById(2);

            Assert.AreEqual(InitialCount - 2, testView.Count);

            AddNew();

            Assert.AreEqual(InitialCount - 1, testView.Count);

            AddNew();

            Assert.AreEqual(InitialCount, testView.Count);
        }
        
        TestEntity Current
        {
            get
            {
                return testView.CurrentItem as TestEntity;
            }
        }

        void CheckCurrentIndex(int index)
        {
            Assert.AreEqual(index + 1, Current.Id);

            Assert.AreEqual(index, testView.CurrentPosition);
        }

        void CheckTrivialCollection(bool forward = true)
        {
            for (int i = 0; i < InitialCount; ++i)
            {
                int index = i;

                TestEntity item = testView.GetItemAt(i) as TestEntity;

                if (forward)
                {
                    Assert.AreEqual(index + 1, item.Id);
                }
                else
                {
                    Assert.AreEqual(InitialCount - index, item.Id);
                }
            }
        }
        
        void MoveCurrentToPosition(int index)
        {
            testView.MoveCurrentToPosition(index);

            CheckCurrentIndex(index);
        }
        
        [TestMethod]
        public void PositionTest()
        {
            Assert.AreEqual(InitialCount, testView.Count);

            MoveCurrentToPosition(0);
            MoveCurrentToPosition(5);
            MoveCurrentToPosition(9);

            testView.MoveCurrentToPosition(-1);

            Assert.IsTrue(Current == null);

            //index of the last element should decrease in when 3 is deleted
            
            MoveCurrentToPosition(InitialCount - 1);

            Assert.AreEqual(InitialCount, Current.Id);

            Assert.AreEqual(InitialCount - 1, testView.CurrentPosition);

            testTable.RemoveById(3);

            Assert.AreEqual(InitialCount, Current.Id);

            Assert.AreEqual(InitialCount - 2, testView.CurrentPosition);
        }

        [TestMethod]
        public void CurrentTest()
        {
            Assert.AreEqual(InitialCount, testView.Count);

            testView.MoveCurrentToFirst();

            CheckCurrentIndex(0);

            testView.MoveCurrentToNext();

            CheckCurrentIndex(1);

            testView.MoveCurrentToLast();

            CheckCurrentIndex(InitialCount - 1);

            testView.MoveCurrentToPrevious();

            CheckCurrentIndex(InitialCount - 2);
        }

        [TestMethod]
        public void ItemPlaceholderTest()
        {
            Assert.AreEqual(InitialCount, testView.Count); //initially it is None

            testView.NewItemPlaceholderPosition = System.ComponentModel.NewItemPlaceholderPosition.AtEnd;

            Assert.AreEqual(InitialCount + 1, testView.Count);

            testView.NewItemPlaceholderPosition = System.ComponentModel.NewItemPlaceholderPosition.AtBeginning;

            Assert.AreEqual(InitialCount + 1, testView.Count);

            testView.NewItemPlaceholderPosition = System.ComponentModel.NewItemPlaceholderPosition.None;

            Assert.AreEqual(InitialCount, testView.Count);
        }

        [TestMethod]
        public void SortTest()
        {
            Assert.AreEqual(InitialCount, testView.Count);

            CheckTrivialCollection();

            TestEntity item = testTable.RemoveById(3);

            testTable.Add(item);

            testView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

            CheckTrivialCollection();

            using (var defer = testView.DeferRefresh())
            {
                testView.SortDescriptions.Clear();

                testView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Descending));
                
                testView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

                item = testTable.RemoveById(3);

                testTable.Add(item);
            }

            CheckTrivialCollection(false);

            testView.SortDescriptions.Clear();

            CheckTrivialCollection();
        }

        [TestMethod]
        public void FilterTest()
        {
            Assert.AreEqual(InitialCount, testView.Count);

            testView.Filter = delegate(object item)
            {
                return true;
            };
            
            Assert.AreEqual(InitialCount, testView.Count);

            testView.Filter = delegate(object item)
            {
                return false;
            };

            Assert.AreEqual(0, testView.Count);

            testView.Filter = delegate(object item)
            {
                TestEntity target = item as TestEntity;

                return target.Id % 2 == 0;
            };

            Assert.AreEqual(5, testView.Count);

            AddNew(15);
            AddNew(16);
            AddNew(17);

            Assert.AreEqual(6, testView.Count);
        }

        [TestMethod]
        public void PropertyChangedTest()
        {
            Assert.AreEqual(InitialCount, testView.Count);

            using (var defer = testView.DeferRefresh())
            {
                testView.SortDescriptions.Add(new SortDescription("IsChecked", ListSortDirection.Ascending));
                testView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
            }

            {
                TestEntity item = testTable.FindById(3);

                item.IsChecked = true;

                TestEntity existing_item = testTable.FindById(3);

                Assert.AreEqual(3, existing_item.Id);

                TestEntity moved_item = testView.GetItemAt(0) as TestEntity;

                Assert.AreEqual(1, moved_item.Id); //IsChecked == false goes first

                TestEntity next_item = testView.GetItemAt(1) as TestEntity;

                Assert.AreEqual(2, next_item.Id);

                next_item = testView.GetItemAt(9) as TestEntity;

                Assert.AreEqual(3, next_item.Id); //IsChecked == true goes last
            }

            using (var defer = testView.DeferRefresh())
            {
                testView.SortDescriptions.Clear();
                testView.SortDescriptions.Add(new SortDescription("IsChecked", ListSortDirection.Descending));
                testView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
            }

            {
                Assert.AreEqual(InitialCount, testView.Count);

                TestEntity item = testTable.FindById(3);

                item.IsChecked = true;

                TestEntity existing_item = testTable.FindById(3);

                Assert.AreEqual(3, existing_item.Id);

                TestEntity moved_item = testView.GetItemAt(0) as TestEntity;

                Assert.AreEqual(3, moved_item.Id); //IsChecked == true goes first

                TestEntity next_item = testView.GetItemAt(1) as TestEntity;

                Assert.AreEqual(1, next_item.Id);

                Assert.AreEqual(InitialCount, testView.Count);

                next_item = testView.GetItemAt(9) as TestEntity;

                Assert.AreEqual(10, next_item.Id); //IsChecked == false goes last
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException), "TableView did not reported that it unable to reset.")]
        public void ResetTest()
        {
            testTable.Clear();
        }
    }
}
