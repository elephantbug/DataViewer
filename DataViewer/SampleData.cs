using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataEngine;

namespace DataViewer
{
    public enum TargetGroup
    {
        None,
        Own,
        Enemy
    }

    [Serializable]
    public class Target : Entity
    {
        public Target()
        {
        }
        
        public Target(long id)
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

        TargetGroup group = TargetGroup.None;

        public TargetGroup Group
        {
            get
            {
                return group;
            }

            set
            {
                if (group != value)
                {
                    using (PropertyUpdateManager.StartUpdate(this, () => Group))
                    {
                        group = value;
                    }
                }
            }
        }
    }
    
    public class SampleData
    {
        Table<Target> targetTable = new Table<Target>();

        TableView<Target> targetsView;

        public SampleData()
        {
            for (int i = 0; i < 102; ++i)
            {
                targetTable.Add(
                    new Target(i + 1) { 
                        Name = String.Format("target-{0}", i + 1) });
            }

            targetsView = new TableView<Target>(targetTable);
        }

        static SampleData theInstance;
        
        public static SampleData Instance
        {
            get
            {
                if (theInstance == null)
                {
                    theInstance = new SampleData();
                }

                return theInstance;
            }
        }
        
        public Table<Target> TargetTable
        {
            get
            {
                return targetTable;
            }
        }

        public TableView<Target> Targets
        {
            get
            {
                return targetsView;
            }

            set
            {
                targetsView = value;
            }
        }
    }
}
