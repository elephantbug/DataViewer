using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace DataEngine
{
    public interface ITransactObject
    {
        bool CanEdit { get; }

        bool IsEditing { get; }

        /// <summary>
        /// Initiates the edit transaction.
        /// </summary>
        /// <param name="table_view">TableView that initiates the transaction</param>
        void BeginEdit(object site);

        void CommitEdit();

        void CancelEdit();
    }

    public interface IEntity
    {
        long Id { get; }

        bool IsNew { get; }

        bool IsUpdating { get; set; }

        void Save(Dictionary<string, object> changed_properties);
    }

    [Serializable]
    public abstract class Entity : NotificationObject, IEntity, ITransactObject, IComparable, ICloneable
    {
        //used by EntityTable to eliminate the need of allocation additinal memory for SingleLink<Entity>
        //so Entity can belong to only one Table at a time
        internal Entity Next { get; set; }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            //objects of absolutelly different types, for example
            //Product and User should always be different

            if (this.GetType().Equals(obj.GetType()))
            {
                Entity right = obj as Entity;

                //Probably we could allow NullReferenceException at this point.
                //It would mean that the HashSet contains an object that 
                //is not dirived from Entity

                if (right == null)
                {
                    throw new NullReferenceException("Probably the Table contains an object that is not dirived from Entity.");
                }
                
                return Id.Equals(right.Id);
            }

            return false;
        }

        public override string ToString()
        {
            return String.Format("Entity.Id = {0}", Id);
        }

        public abstract long Id
        {
            get;
        }
        
        public bool IsNew
        {
            get { return Id == 0; }
        }

        bool isUpdating;

        public bool IsUpdating
        {
            get { return isUpdating; }

            set
            {
                if (isUpdating != value)
                {
                    using (PropertyUpdateManager.StartUpdate(this, () => IsUpdating))
                    {
                        isUpdating = value;
                    }
                }
            }
        }

        public void Save(Dictionary<string, object> changed_properties)
        {
        }

        public object Clone()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter(null, new StreamingContext(StreamingContextStates.All, null));

                formatter.Serialize(stream, this);

                stream.Seek(0, SeekOrigin.Begin);

                return formatter.Deserialize(stream);
            }
        }

        public bool CanEdit
        {
            get
            {
                return !IsUpdating;
            }
        }

        public bool IsEditing
        {
            get
            {
                return EditTransaction.Current.EditingObject == this;
            }
        }

        public void BeginEdit(object site)
        {
            //DataGrid no loger calls BeginEdit two times
            //So we do not need check to see if BeginEdit has already been called. 

            if (EditTransaction.Current.EditingObject != null)
            {
                throw new InvalidOperationException("ViewMolel.BeinEdit: Only one object could be edited at a time.");
            }

            EditTransaction.Current.BeginEdit(site, this);

            RaisePropertyChanged(() => IsEditing);
        }

        public void CommitEdit()
        {
            if (!IsEditing)
            {
                throw new InvalidOperationException("Incorrect EndEdit() call.");
            }

            //ViewModel asks the Table to save the Entity
            //and "waits" until ViewList will receive the notification
            //Site.OnEntitySaved(Entity);

            //save it before passing to other thread
            Dictionary<string, object> changed_properties = EditTransaction.Current.ChangedProperties;

            //this erases ChangedProperties
            EditTransaction.Current.EndEdit(this);

            if (changed_properties != null)
            {
                Save(changed_properties);
            }

            RaisePropertyChanged(() => IsEditing);
        }

        public void CancelEdit()
        {
            if (!IsEditing)
            {
                throw new InvalidOperationException("Incorrect CancelEdit() call.");
            }

            Entity backup = EditTransaction.Current.CancelEdit(this) as Entity;

            //InternalSetEntity(backup);

            RaisePropertyChanged(() => IsEditing);
        }

        public int CompareTo(object obj)
        {
            Entity entiry = obj as Entity;

            return Id.CompareTo(entiry.Id);
        }
    }
}
