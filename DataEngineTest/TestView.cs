using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataEngine;

namespace DataEngineTest
{
    class TestView : TableView<TestEntity>
    {
        public static int InstanceCount = 0;
        
        public TestView(Table<TestEntity> table) : base(table)
        {
            ++InstanceCount;
        }

        ~TestView()
        {
            --InstanceCount;
        }
    }
}
