using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataEngineTest
{
    [TestClass]
    public class EntityTest
    {
        [TestMethod]
        public void EqualityTest()
        {
            TestEntity a = new TestEntity(1);

            TestEntity b = new TestEntity(1);
            
            TestEntity c = new TestEntity(2);

            Assert.IsTrue(a.Equals(b));

            Assert.IsFalse(a.Equals(c));
        }
    }
}
