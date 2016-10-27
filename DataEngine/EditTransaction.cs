using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataEngine
{
    class EditTransaction
    {
        object viewSite;

        public object ViewSite
        {
            get { return viewSite; }
        }

        object editingObject;

        public object EditingObject
        {
            get { return editingObject; }
            
            set
            {
                if (editingObject != null)
                {
                    System.ComponentModel.INotifyPropertyChanged npc = editingObject as System.ComponentModel.INotifyPropertyChanged;

                    npc.PropertyChanged -= RowPropertyChanged;
                }

                editingObject = value;

                if (editingObject != null)
                {
                    System.ComponentModel.INotifyPropertyChanged npc = editingObject as System.ComponentModel.INotifyPropertyChanged;

                    if (npc == null)
                    {
                        throw new Exception("Editing entity does not implement INotifyPropertyChanged.");
                    }

                    npc.PropertyChanged += RowPropertyChanged;
                }
            }
        }

        public bool InProgress
        {
            get
            {
                return editingObject != null;
            }
        }
        
        object backupObject;

        Dictionary<string, object> changedProperties;

        public Dictionary<string, object> ChangedProperties
        {
            get { return changedProperties; }
        }
        
        Logger Logger;

        static EditTransaction currentTransaction = new EditTransaction();

        public EditTransaction()
        {
            Logger = LoggerFactory.CreateLogger("EditTransaction");

            Logger.Level = LogLevel.Debug;
        }
        
        public static EditTransaction Current
        {
            get
            {
                return currentTransaction;
            }
        }

        object InternalEndEdit(object edit)
        {
            if (edit == null || editingObject == null || viewSite == null)
            {
                throw new InvalidOperationException("BeginEdit should be called before EndEdit or CancelEdit.");
            }
            else
            {
                if (editingObject != edit)
                {
                    throw new InvalidOperationException("Incorrect editing object provided for EndEdit or CancelEdit.");
                }
            }

            viewSite = null;

            changedProperties = null;

            EditingObject = null; //unsubscribe from NPC

            object tmp = backupObject;

            backupObject = null;

            return tmp;
        }

        public void BeginEdit(object view_site, object entity)
        {
            //DataGrid calls BeginEdit two times, but ViewModel should check this

            if (viewSite != null || editingObject != null
                || backupObject != null || changedProperties != null)
            {
                throw new InvalidOperationException("There could be only one editing object at a time.");
            }

            viewSite = view_site;

            EditingObject = entity; //subscribe to NPC

            //it doesn't matter whether we are adding a new row or editing existing row
            //we always make a deep copy of the entity

            {
                ICloneable cloneable = editingObject as ICloneable;

                if (cloneable == null)
                {
                    throw new Exception("Editing entity does not implement ICloneable.");
                }

                backupObject = cloneable.Clone();
            }
        }

        public void EndEdit(object view)
        {
            InternalEndEdit(view);
        }

        public object CancelEdit(object view)
        {
            return InternalEndEdit(view);
        }

        void SetEntity(object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }
            
            if (editingObject == null || viewSite == null)
            {
                throw new InvalidOperationException(
                    "Entity can be set only within an active Edit Transaction. Otherwise the Entity will not be saved.");
            }

            EditingObject = entity;
        }

        //public bool CheckEdititngEntity(object entity)
        //{
        //    if (editingView.Entity.Equals(entity))
        //    {
        //        System.Windows.MessageBox.Show("Object that is currently edited is being updated.", App.Title);

        //        return true;
        //    }

        //    return false;
        //}

        void RowPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (changedProperties == null)
            {
                //it can collect changes from multiple editing objects (if SetEntity is called). ??? what does it mean?
                changedProperties = new Dictionary<string, object>();
            }
            
            if (!changedProperties.ContainsKey(e.PropertyName))
            {
                object value = sender.GetType().GetProperty(e.PropertyName).GetValue(sender, null);

                changedProperties.Add(e.PropertyName, value);

                Logger.Print("Property changed: Name={0}, Value={1}", e.PropertyName, value);
            }
        }
    }
}
