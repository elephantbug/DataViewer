using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataEngine;

namespace DataEngineTest
{
    class TestEntity : Entity
    {
        public TestEntity()
        {
        }
        
        public TestEntity(long id)
        {
            this.id = id;
        }

        long id = 0;

        public override long Id
        {
            get
            {
                return id;
            }
        }

        string name;

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                if (name != value)
                {
                    using (PropertyUpdateManager.StartUpdate(this, () => Name))
                    {
                        name = value;
                    }
                }
            }
        }

        bool isChecked = false;

        public bool IsChecked
        {
            get
            {
                return isChecked;
            }

            set
            {
                if (isChecked != value)
                {
                    using (PropertyUpdateManager.StartUpdate(this, () => IsChecked))
                    {
                        isChecked = value;
                    }
                }
            }
        }
    }

    /*
    class UniqueEntity : Entity
    {
        public override bool Equals(object obj)
        {
            return object.ReferenceEquals(this, obj);
        }
    }*/
}
