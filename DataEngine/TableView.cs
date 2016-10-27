using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Data;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;
using System.Reflection;
using System.Diagnostics;
using DataEngine.Collections;

namespace DataEngine
{
    /// <summary>
    /// TableView cannot implement IViewModelSite because
    /// it does not know the entity type and ideally it 
    /// should not deal with the entities at all.
    /// ITransactObject "hides" the entity type.
    /// Unfortunately, new(TEntity) isn't allowed in C#.
    /// </summary>
    /// <typeparam name="TView"></typeparam>
    public class TableView<TView> : NotificationObject, ICollectionView, IEnumerable, IEditableCollectionViewAddNewItem, IEditableCollectionView,
        INotifyCollectionChanged, IItemProperties
        where TView : Entity, new()
    {
        #region Constructors

#if DEBUG
        Logger Logger;
#endif
        public TableView()
        {
#if DEBUG
            Logger = LoggerFactory.CreateLogger(String.Format("TableView<{0}>",
                typeof(TView).Name));

            Logger.Level = LogLevel.Debug;
#endif

            rbTree = new RBOrderedTree<TView>();
        }
        
        public TableView(Table<TView> table) : this()
        {
            Table = table;

            CollectionChangedEventManager.AddHandler(table, OnTableCollectionChanged);

#if DEBUG
            Logger = LoggerFactory.CreateLogger(String.Format("TableView<{0}>",
                typeof(TView).Name));

            Logger.Level = LogLevel.Debug;
#endif

            //TableView can remain empty until ItemsControl does DeferRefresh
            //when it attaches to TableView, but in this case the event handling 
            //also should be defered.

            LazyRefresh(); //it will populate the items collection
        }

        ~TableView()
        {
            CollectionChangedEventManager.RemoveHandler(Table, OnTableCollectionChanged);
            
            if (sortDescriptions != null)
            {
                CollectionChangedEventManager.RemoveHandler(sortDescriptions, OnSortDescriptionCollectionCollectionChanged);
            }
        }

        #endregion Constructors

        #region Weak Event Listener Members

        void OnSortDescriptionCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SetFlag(CollectionViewFlags.SortChanged, true);

            LazyRefresh();
        }

        void HandleAdd(IList new_items)
        {
            foreach (object untyped_item in new_items)
            {
                TView item = untyped_item as TView;

                RegisterItem(item);

                if (PassesFilter(item))
                {
                    rbTree.Add(item);

                    int index = rbTree.IndexOfKey(item);

                    RaiseCollectionChanged(
                        new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Add,
                            item,
                            index));
                }
            }
        }
        
        void HandleRemove(IList new_items)
        {
            foreach (object untyped_item in new_items)
            {
                TView item = untyped_item as TView;

                //If the item passes the filter it sould exist in the collection.
                //If it does not exist, it indicates that the user does not work
                //properly with the filter.
                if (PassesFilter(item))
                {
                    int index = rbTree.IndexOfKey(item);

                    if (!rbTree.Remove(item) || index == -1)
                    {
                        throw new InvalidOperationException(String.Format(
                            "Item {0} removed from Table not found in TableView. Probably it was filtered out without refreshing TableView.",
                            item));
                    }

                    RaiseCollectionChanged(
                        new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Remove,
                            item,
                            index));

                    //Probably current item is removed here?
                    //I suppose that ListView will change CurrentItem automatically... but I am not sure...
                    if (currentItem != null && Object.ReferenceEquals(item, currentItem))
                    {
                        InternalMoveCurrentTo(null);
                    }
                }
                
                //The item should be unregistered independantly of
                //whether it exists in rbTree or not, because 
                //we do need to listen this item events anymore.
                UnregisterItem(item);
            }
        }

        void OnTableCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
#if DEBUG
            Logger.Print("Collection changed. Action:{0}, new:{1}, old:{2}", e.Action,
                e.NewItems != null ? e.NewItems[0] : "null",
                e.OldItems != null ? e.OldItems[0] : "null");
#endif

            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    {
                        HandleAdd(e.NewItems);

                        break;
                    }
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    {
                        HandleRemove(e.OldItems);

                        break;
                    }

                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    {
                        HandleAdd(e.NewItems);
                        HandleRemove(e.OldItems);

                        break;
                    }

                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    {
                        break;
                    }

                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    {
                        throw new InvalidOperationException("NotifyCollectionChangedAction.Reset is not supported by TableView (do not call Table.Clear()).");
                        
                        //we will loose subscribed item here... 
                        //probably we can access them somehow after some further
                        //investigation of WeakEventManager
                        //SetFlag(CollectionViewFlags.SourceCollectionChanged, true);
                        //LazyRefresh();
                        //break;
                    }
            }
        }

        #endregion

        #region IItemProperties

        /// <summary>
        /// Returns information about the properties available on items in the
        /// underlying collection.  This information may come from a schema, from
        /// a type descriptor, from a representative item, or from some other source
        /// known to the view.
        /// </summary>
        public ReadOnlyCollection<ItemPropertyInfo> ItemProperties
        {
            get
            {
                Type type = typeof(TView);

                PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                ItemPropertyInfo[] infos = new ItemPropertyInfo[props.Length];

                for (int i = 0; i < props.Length; ++i)
                {
                    PropertyInfo prop = props[i];

                    infos[i] = new ItemPropertyInfo(prop.Name, prop.PropertyType, null);
                }

                return new ReadOnlyCollection<ItemPropertyInfo>(infos);
            }
        }

        #endregion IItemProperties

        #region ICollectionView Implementation

        public bool CanFilter
        {
            get { return true; }
        }

        public bool CanGroup
        {
            get { return false; }
        }

        public bool CanSort
        {
            get { return true; }
        }

        public bool Contains(object item)
        {
            //ListView can pass null here
            if (item == null)
            {
                return false;
            }
            
            return rbTree.Find(Object2T(item)) != null;
        }

        public System.Globalization.CultureInfo Culture
        {
            get
            {
                return System.Threading.Thread.CurrentThread.CurrentCulture;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public object CurrentItem
        {
            get
            {
                return currentItem;
            }
        }

        public int CurrentPosition
        {
            get 
            {
                if (currentItem == null)
                {
                    return -1;
                }

                int index = rbTree.IndexOfKey(currentItem);

                //CurrentPosition should not be used internally by MoveTo...
                //if (index == -1)
                //{
                //    throw new InvalidOperationException("CurrentItem does not belong to the collection.");
                //}

                return index;
            }
        }

        public IDisposable DeferRefresh()
        {
            if (IsEditingOrAddingNew)
            {
                throw new InvalidOperationException("DeferRefresh is not allowed during adding new item or editing.");
            }

            ++deferLevel;

            return new DeferHelper(this);
        }

        public bool IsEmpty
        {
            get { return this.rbTree.Count == 0; }
        }

        public Predicate<object> Filter
        {
            get
            {
                return tableFilter;
            }
            set
            {
                tableFilter = value;

                SetFlag(CollectionViewFlags.FilterChanged, true);

                LazyRefresh();
            }
        }

        public Predicate<string> PropertyFilter
        {
            get
            {
                return propertyFilter;
            }
            set
            {
                propertyFilter = value;

                SetFlag(CollectionViewFlags.FilterChanged, true);

                LazyRefresh();
            }
        }

        /// <summary>
        /// Should be called along with LazyRefresh() after the global
        /// filter conditions that affect entire collection
        /// were changed without changing the predicate object.
        /// </summary>
        public void InvalidateFilter()
        {
            SetFlag(CollectionViewFlags.FilterChanged, true);
        }

        public ObservableCollection<GroupDescription> GroupDescriptions
        {
            get { return null; }
        }

        public System.Collections.ObjectModel.ReadOnlyObservableCollection<object> Groups
        {
            get { return null; }
        }

        /// <summary>
        /// //IsCurrent... methods are never called, so they are not tested
        /// </summary>
        public bool IsCurrentAfterLast
        {
            get { return false; }
        }

        /// <summary>
        /// //IsCurrent... methods are never called, so they are not tested
        /// </summary>
        public bool IsCurrentBeforeFirst
        {
            get { return this.CurrentItem == null || rbTree.Find(currentItem) == null; }
        }

        /// <summary>
        /// Item can be null. Null can be set from TargetsLayer when selected target is lost.
        /// Allows to move to the item that does not belong to TableView, assuming that 
        /// Refresh will check it later and reset it to null if needed.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool MoveCurrentTo(object item)
        {
            TView view = item as TView; //view can be null, so we do not use Object2T(item)

            //this check can slow down selection in ItemsControl a bit
            //if (item != null && rbTree.Find(view) == null)
            //{
            //    throw new InvalidOperationException(String.Format(
            //        "Attempt to move current item to '{0}' that does not belong to the TableView",
            //        view));
            //}
            
            return InternalMoveCurrentTo(item as TView);
        }

        public bool MoveCurrentToFirst()
        {
            ITreeNode<TView> node = rbTree.First();

            if (node != null)
            {
                return InternalMoveCurrentTo(node.Key);
            }

            return false;
        }

        public bool MoveCurrentToLast()
        {
            ITreeNode<TView> node = rbTree.Last();

            if (node != null)
            {
                return InternalMoveCurrentTo(node.Key);
            }

            return false;
        }

        public bool MoveCurrentToNext()
        {
            if (currentItem == null)
            {
                return false;
            }

            ITreeNode<TView> node = rbTree.Find(currentItem);

            ITreeNode<TView> next = rbTree.Next(node);

            if (next != null)
            {
                return InternalMoveCurrentTo(next.Key);
            }

            return false;
        }

        public bool MoveCurrentToPrevious()
        {
            if (currentItem == null)
            {
                return false;
            }

            ITreeNode<TView> node = rbTree.Find(currentItem);

            ITreeNode<TView> prev = rbTree.Previous(node);

            if (prev != null)
            {
                return InternalMoveCurrentTo(prev.Key);
            }

            return false;
        }

        // true if CurrentPosition points to item within view
        bool IsCurrentInView
        {
            get
            {
                if (currentItem == null)
                {
                    return false;
                }
                
                return rbTree.Find(currentItem) != null;
            }
        }

        public bool MoveCurrentToPosition(int position)
        {
            VerifyRefreshNotDeferred();

            if (position < -1 || position > InternalCount)
            {
                throw new ArgumentOutOfRangeException("position");
            }

            TView position_item; //find item at position
            
            if (position == -1)
            {
                position_item = null;
            }
            else
            {
                position_item = rbTree.KeyByIndex(position);
            }

            return InternalMoveCurrentTo(position_item);
        }

        public void Refresh()
        {
            if (IsEditingOrAddingNew)
            {
                throw new InvalidOperationException("Refresh is not allowed during add or edit.");
            }

            if (IsRefreshNeeded)
            {
                DoRefresh();

                ClearRefreshFlags();
            }
            else
            {
                //collection was not changed
                //but ItemsControl needs NotifyCollectionChangedAction.Reset to be sent
                //when it initially attaches to the collection
                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// Should be used in the user code along with InvalidateFilter() 
        /// to avoid superfluous refreshes.
        /// </summary>
        public void LazyRefresh()
        {
            if (!IsRefreshDeferred)
            {
                Refresh();
            }
        }

        SortDescriptionCollection sortDescriptions;
        
        public SortDescriptionCollection SortDescriptions
        {
            get 
            {
                if (sortDescriptions == null)
                {
                    sortDescriptions = new SortDescriptionCollection();

                    CollectionChangedEventManager.AddHandler(sortDescriptions, OnSortDescriptionCollectionCollectionChanged);
                }
                
                return sortDescriptions; 
            }
        }

        public System.Collections.IEnumerable SourceCollection
        {
            get 
            {
#if DEBUG
                Logger.Print("SourceCollection.get called");
#endif

                return Table;
            }
        }

#if DEBUG
        int iterCount = 0;
#endif

        public System.Collections.IEnumerator GetEnumerator()
        {
            //it should be a NewItemPlaceholder aware enumerator
            //i. e. it should iterate through all the items including
            //Placeholder, New Item and normal (internal) items

            //!IsAddingNew && 
            if (newItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning)
            {
                yield return NewItemPlaceholder;
            }

            foreach (TView item in rbTree.Collection)
            {
                yield return item;
            }

            if (newItemPlaceholderPosition == NewItemPlaceholderPosition.AtEnd)
            {
                yield return NewItemPlaceholder;
            }

#if DEBUG
            iterCount++;

            Logger.Print("Finished iterating, iterCount={0}.", iterCount);
#endif

            //return new TableViewEnumerator<TView>(viewList, newItemPlaceholderPosition);
        }

        public event CurrentChangingEventHandler CurrentChanging;

        /// <summary>
        /// Raises the CurrentChanging event
        /// </summary>
        /// <param name="args">
        ///     CancelEventArgs used by the consumer of the event.  args.Cancel will
        ///     be true after this call if the CurrentItem should not be changed for
        ///     any reason.
        /// 
        /// <exception cref="InvalidOperationException">
        ///     This CurrentChanging event cannot be canceled.
        /// </exception>
        protected virtual void RaiseCurrentChanging(CurrentChangingEventArgs args)
        {
            if (_currentChangedMonitor.Busy)
            {
                if (args.IsCancelable)
                {
                    args.Cancel = true;

                    return;
                }
            }

            if (CurrentChanging != null)
            {
                CurrentChanging(this, args);
            }
        }

        public event EventHandler CurrentChanged;

        /// <summary>
        /// Raises the CurrentChanged event
        /// </summary>
        protected virtual void RaiseCurrentChanged()
        {
            if (CurrentChanged != null && _currentChangedMonitor.Enter())
            {
                using (_currentChangedMonitor)
                {
                    CurrentChanged(this, EventArgs.Empty);
                }
            }
        }

        public event System.Collections.Specialized.NotifyCollectionChangedEventHandler CollectionChanged;

        protected void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }

            if (e.Action != NotifyCollectionChangedAction.Move && e.Action != NotifyCollectionChangedAction.Replace)
            {
                RaisePropertyChanged(() => Count);
            }
        }

        #endregion ICollectionView Implementation

        #region IEditableCollectionViewAddNewItem implementation

        public object AddNewItem(object newItem)
        {
            CheckBeforeAddNew();

            return AddNewCommon(Object2T(newItem));
        }

        public bool CanAddNewItem
        {
            get { return !IsEditingItem; }
        }

        public object AddNew()
        {
            CheckBeforeAddNew();

            return AddNewCommon(CreateNewView());
        }

        public bool CanAddNew
        {
            get { return !IsEditingItem; }
        }

        /// <summary>
        /// Original implementation checks whether editItem is IEditableObject
        /// </summary>
        public bool CanCancelEdit
        {
            get { return true; }
        }

        public bool CanRemove
        {
            get { return !IsEditingItem && !IsAddingNew; }
        }

        public void CancelEdit()
        {
            VerifyBeforeEndEdit();

            editItem.CancelEdit();

            editItem = null;

            RaiseItemEvents();
        }

        public void CancelNew()
        {
            throw new NotImplementedException();
            
            /*
            if (IsEditingItem)
            {
                throw new InvalidOperationException(Format(MemberNotAllowedDuringTransaction, "CancelNew", "EditItem"));
            }

            VerifyRefreshNotDeferred();

            if (editItem == null)
            {
                throw new InvalidOperationException("CancelNew called without active transaction.");
            }

            int new_index = NewItemIndex - 1;

            // remove the new item from the underlying collection. Normally the
            // collection will raise a Remove event, which we'll handle by calling
            // EndNew to leave AddNew mode.
            rbTree.RemoveAt(NewItemIndex - 1);

            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove,
                editItem,
                new_index));

            editItem.CancelEdit();

            editItem = null;
            
            RaiseItemEvents(); //null, false
            */
        }

        public void CommitEdit()
        {
            VerifyBeforeEndEdit();

            editItem.CommitEdit();

            editItem = null;

            RaiseItemEvents();
        }

        /// <summary>
        /// Complete the transaction started by AddNew". The new
        /// item remains in the collection, and the view's sort, filter, and grouping
        /// specifications (if any) are applied to the new item.
        /// </summary>
        public void CommitNew()
        {
            if (IsEditingItem)
            {
                throw new InvalidOperationException(Format(MemberNotAllowedDuringTransaction, "CommitNew", "EditItem"));
            }

            VerifyRefreshNotDeferred();

            if (editItem == null)
            {
                throw new InvalidOperationException("CommitNew called without active transaction.");
            }

            editItem.CommitEdit();

            editItem = null;

            RaiseItemEvents(); //null, false
        }

        public object CurrentAddItem
        {
            get
            {
                if (IsAddingNew)
                {
                    return editItem;
                }
                
                return null;
            }
        }

        public object CurrentEditItem
        {
            get
            {
                if (IsEditingItem)
                {
                    return editItem;
                }

                return null;
            }
        }

        public void EditItem(object item)
        {
            VerifyRefreshNotDeferred();

            if (item == NewItemPlaceholder)
            {
                throw new ArgumentException("Cannot Edit Placeholder");
            }

            bool assign = true;
            
            if (editItem != null)
            {
                //DataGrid called EditItem just after AddNew
                if (Object.Equals(editItem, item) && IsAddingNew)
                {
                    //so we do not need to call editItem.BeginEdit() again
                    assign = false;
                }
                else
                {
                    throw new InvalidOperationException("Double edit transaction.");
                }
            }

            if (assign)
            {
                editItem = Object2T(item);

                editItem.BeginEdit(this);
            }

            RaiseItemEvents(); //Object2T(item), true
        }

        public bool IsAddingNew
        {
            get
            {
                return editItem != null && editItem.IsNew;
            }
        }

        public bool IsEditingItem
        {
            get
            {
                return editItem != null && !editItem.IsNew;
            }
        }

        public NewItemPlaceholderPosition NewItemPlaceholderPosition
        {
            get
            {
                return newItemPlaceholderPosition;
            }
            set
            {
                VerifyRefreshNotDeferred();

                if (newItemPlaceholderPosition != value)
                {
                    if (IsAddingNew)
                    {
                        throw new InvalidOperationException("Changing NewItemPlaceholderPosition is not allowed during AddNew transaction");
                    }

                    NotifyCollectionChangedEventArgs args = null;

                    int old_index = -1, new_index = -1;

                    // we're adding, removing, or moving the placeholder.
                    // Determine the appropriate events.
                    switch (value)
                    {
                        case NewItemPlaceholderPosition.None:
                            switch (newItemPlaceholderPosition)
                            {
                                case NewItemPlaceholderPosition.None:
                                    break;
                                case NewItemPlaceholderPosition.AtBeginning:
                                    old_index = 0;
                                    args = new NotifyCollectionChangedEventArgs(
                                                    NotifyCollectionChangedAction.Remove,
                                                    NewItemPlaceholder,
                                                    old_index);
                                    break;
                                case NewItemPlaceholderPosition.AtEnd:
                                    old_index = InternalCount;
                                    args = new NotifyCollectionChangedEventArgs(
                                                    NotifyCollectionChangedAction.Remove,
                                                    NewItemPlaceholder,
                                                    old_index);
                                    break;
                            }
                            break;

                        case NewItemPlaceholderPosition.AtBeginning:
                            switch (newItemPlaceholderPosition)
                            {
                                case NewItemPlaceholderPosition.None:
                                    new_index = 0;
                                    args = new NotifyCollectionChangedEventArgs(
                                                    NotifyCollectionChangedAction.Add,
                                                    NewItemPlaceholder,
                                                    new_index);
                                    break;
                                case NewItemPlaceholderPosition.AtBeginning:
                                    break;
                                case NewItemPlaceholderPosition.AtEnd:
                                    old_index = InternalCount;
                                    new_index = 0;
                                    args = new NotifyCollectionChangedEventArgs(
                                                    NotifyCollectionChangedAction.Move,
                                                    NewItemPlaceholder,
                                                    new_index,
                                                    old_index);
                                    break;
                            }
                            break;

                        case NewItemPlaceholderPosition.AtEnd:
                            switch (newItemPlaceholderPosition)
                            {
                                case NewItemPlaceholderPosition.None:
                                    new_index = InternalCount;
                                    args = new NotifyCollectionChangedEventArgs(
                                                    NotifyCollectionChangedAction.Add,
                                                    NewItemPlaceholder,
                                                    new_index);
                                    break;
                                case NewItemPlaceholderPosition.AtBeginning:
                                    old_index = 0;
                                    new_index = InternalCount;
                                    args = new NotifyCollectionChangedEventArgs(
                                                    NotifyCollectionChangedAction.Move,
                                                    NewItemPlaceholder,
                                                    new_index,
                                                    old_index);
                                    break;
                                case NewItemPlaceholderPosition.AtEnd:
                                    break;
                            }
                            break;
                    }

                    newItemPlaceholderPosition = value;

                    RaiseCollectionChanged(args);

                    RaisePropertyChanged(() => NewItemPlaceholderPosition);
                }
            }
        }

        public void Remove(object item)
        {
            var t_item = Object2T(item);
            
            int index = rbTree.IndexOfKey(t_item);

            rbTree.Remove(t_item);

            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Remove, item, index));
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        #endregion IEditableCollectionViewAddNewItem implementation

        #region Private Types

        [Flags]
        private enum CollectionViewFlags
        {
            //UpdatedOutsideDispatcher = 0x2,
            //ShouldProcessCollectionChanged = 0x4,
            //IsCurrentBeforeFirst = 0x8,
            //IsCurrentAfterLast = 0x10,
            //IsDynamic = 0x20,
            //IsDataInGroupOrder = 0x40,
            //NeedsRefresh = 0x80, //initial refresh when source collection changes
            //IsMultiThreadCollectionChangeAllowed = 0x100,
            //CachedIsEmpty = 0x200,
            SourceCollectionChanged = 0x200,
            SortChanged = 0x400,
            GroupsChanged = 0x800,
            FilterChanged = 0x1000
        }

        const CollectionViewFlags RefreshMask =
            CollectionViewFlags.SourceCollectionChanged |
            CollectionViewFlags.GroupsChanged |
            CollectionViewFlags.SortChanged |
            CollectionViewFlags.FilterChanged;

        // this class helps prevent reentrant calls
        private class SimpleMonitor : IDisposable
        {
            public bool Enter()
            {
                if (_entered)
                    return false;

                _entered = true;
                return true;
            }

            public void Dispose()
            {
                _entered = false;
                GC.SuppressFinalize(this);
            }

            public bool Busy { get { return _entered; } }

            bool _entered;
        }

        private class DeferHelper : IDisposable
        {
            private TableView<TView> collectionView;

            public DeferHelper(TableView<TView> collection_view)
            {
                collectionView = collection_view;
            }

            public void Dispose()
            {
                collectionView.EndDefer();

                GC.SuppressFinalize(this);
            }
        }

        #endregion

        #region Helper methods

        bool PassesFilter(TView item)
        {
            return tableFilter == null || tableFilter(item);
        }

        TView CreateNewView()
        {
            return new TView();
        }

        TView Object2T(object value)
        {
            TView t = value as TView;

            if (t == null)
            {
                throw new ArgumentException(
                    String.Format("The value '{0}' is not of type '{1}' and cannot be used in this generic collection.",
                    value, typeof(TView).Name));
            }

            return t;
        }

        void EndDefer()
        {
            --deferLevel;

            if (deferLevel == 0)
            {
                Refresh();
            }
        }

        void RegisterItem(TView item)
        {
            //this is probably temporary workaround used to prevent item from being subscribed multiple times
            UnregisterItem(item);
            
            System.Windows.WeakEventManager<TView, PropertyChangingEventArgs>.AddHandler(
                item, "PropertyChanging", OnItemPropertyChanging);

            System.Windows.WeakEventManager<TView, PropertyChangedEventArgs>.AddHandler(
                item, "PropertyChanged", OnItemPropertyChanged);
        }

        void UnregisterItem(TView item)
        {
            System.Windows.WeakEventManager<TView, PropertyChangingEventArgs>.RemoveHandler(
                item, "PropertyChanging", OnItemPropertyChanging);

            System.Windows.WeakEventManager<TView, PropertyChangedEventArgs>.RemoveHandler(
                item, "PropertyChanged", OnItemPropertyChanged);
        }

        //it is not clear how to unsubscribe if, for example, 
        //all the items were removed from the source collection 
        //(coll.Clear() has been called)
        //void RemovePropertyChangeListeners()
        //{
        //    foreach (TView entity in Table)
        //    {
        //        UnregisterItem(entity);
        //    }
        //}


        //moved oldIndex and changingNode back from PropertyUpdateManager because there can be 
        //more than one TableView with different filters

        int oldIndex = -1; //it always -1 if changingObject is TView

        object changingObject; //can be either TNode or TView

        public void SaveChangingObject(object changing_object, int index)
        {
            if (!PropertyUpdateManager.IsUpdateInProgress)
            {
                throw new InvalidOperationException("Attempt to save changing object outside of PropertyUpdateSection.");
            }

            changingObject = changing_object;

            oldIndex = index;
        }

        public object GetChangingObject(out int index)
        {
            if (!PropertyUpdateManager.IsUpdateInProgress)
            {
                throw new InvalidOperationException("Attempt to get changing object outside of PropertyUpdateSection.");
            }

            index = oldIndex;

            object changing_node = changingObject;

            oldIndex = -1;

            changingObject = null;

            return changing_node;
        }

        void SaveChangingItem(TView item)
        {
            ITreeNode<TView> changing_node = rbTree.Find(item);

            if (changing_node != null)
            {
                //changing item belongs to TableView and shown in ListView
                
                int old_index = rbTree.IndexOfKey(item);

                rbTree.Remove(changing_node);

                SaveChangingObject(changing_node, old_index);
            }
            else
            {
                //changing item is filtered out from TableView so we save index == -1

                SaveChangingObject(changing_node, -1);
            }
        }

        void UpdateChangedItem()
        {
            int old_index;

            object changing_object = GetChangingObject(out old_index);

            if (old_index != -1)
            {
                //changing item initally belonged to TableView

                ITreeNode<TView> changing_node = changing_object as ITreeNode<TView>;

                TView item = changing_node.Key;

                if (PassesFilter(item))
                {
                    rbTree.Add(changing_node);

                    int new_index = rbTree.IndexOfKey(item);

                    if (new_index != old_index)
                    {
                        RaiseCollectionChanged(
                            new NotifyCollectionChangedEventArgs(
                                NotifyCollectionChangedAction.Move,
                                item,
                                new_index,
                                old_index));
                    }
                }
                else
                {
                    //Probably current item is removed here?
                    //I suppose that ListView will change CurrentItem automatically... but I am not sure...
                    if (currentItem != null && Object.ReferenceEquals(item, currentItem))
                    {
                        InternalMoveCurrentTo(null);
                    }

                    RaiseCollectionChanged(
                        new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Remove,
                            item,
                            old_index));
                    
                    //the item should not be unregistered here because it can
                    //return back to the TableView if it change its property again
                    //UnregisterItem(item);
                }
            }
            else
            {
                //changing item initally was filtered out from TableView,
                //so there is no TNode

                TView item = changing_object as TView;

                if (PassesFilter(item))
                {
                    rbTree.Add(item);

                    int new_index = rbTree.IndexOfKey(item);

                    RaiseCollectionChanged(
                        new NotifyCollectionChangedEventArgs(
                            NotifyCollectionChangedAction.Add,
                            item,
                            new_index));

                    RegisterItem(item);
                }
            }
        }
        
        bool AffectsSortOrder(string property_name)
        {
            if (sortDescriptions != null)
            {
                foreach (SortDescription d in sortDescriptions)
                {
                    if (d.PropertyName == property_name)
                    {
                        return true;
                    }
                }
            }

            if (tableFilter != null && propertyFilter != null && propertyFilter(property_name))
            {
                return true;
            }

            return false;
        }
        
        void OnItemPropertyChanging(object source, PropertyChangingEventArgs e)
        {
            if (AffectsSortOrder(e.PropertyName))
            {
                SaveChangingItem(source as TView);
            }
        }

        void OnItemPropertyChanged(object source, PropertyChangedEventArgs e)
        {
            if (AffectsSortOrder(e.PropertyName))
            {
                if (changingObject == null)
                {
                    //Changing node is not set. This means that either detached item has been changed or
                    //TView does not support INotifyPropertyChanging.

                    //give it a chance to return to TableView
                    changingObject = source;

                    if (oldIndex != -1)
                    {
                        throw new InvalidOperationException("oldIndex != -1");
                    }
                }
                else
                {
                    //check that source parameter complies with changingObject recieved from INotifyPropertyChanging

                    ITreeNode<TView> node = changingObject as ITreeNode<TView>;

                    if (node != null)
                    {
                        if (node.Key != source)
                        {
                            throw new InvalidOperationException("Wrong value of changingObject.");
                        }
                    }
                    else
                    {
                        TView item = node as TView;

                        if (item != source)
                        {
                            throw new InvalidOperationException("Wrong value of changingObject.");
                        }
                    }
                }
                
                UpdateChangedItem();
            }
        }
        
        void FillItems()
        {
            rbTree.Clear();
            
            foreach (TView item in Table)
            {
                RegisterItem(item);
                
                if (PassesFilter(item))
                {
                    rbTree.Add(item);
                }
            }
        }

        void DoRefresh()
        {
            bool changed = false;
            
            if (CheckFlag(CollectionViewFlags.SourceCollectionChanged) ||
                CheckFlag(CollectionViewFlags.FilterChanged) ||
                CheckFlag(CollectionViewFlags.GroupsChanged))
            {
                FillItems();

                changed = true;
            }

            if (changed || CheckFlag(CollectionViewFlags.SortChanged))
            {
                if (sortDescriptions != null && sortDescriptions.Count > 0)
                {
                    rbTree.Comparer = new DynamicComparer<TView>(sortDescriptions);

                    changed = true;

                    //PropertyInfo prop = typeof(TEntity).GetProperty("Name");

                    //sourceCollection = sourceCollection.OrderBy(v =>
                    //    {
                    //        return prop.GetValue(v.Entity);
                    //    }).ToList();
                }
                else
                {
                    rbTree.Comparer = Comparer<TView>.Default;
                    
                    changed = true;
                }
            }

            if (changed)
            {
                //Initially I thought that there is a bit another logic: CurrentItem lives its own live independently 
                //of the filter and we TableView does not care of whether CurrentItem belongs to the collection or not. 
                //When the filter changes, CurrentItem can remain the same independently of whether it is filtered out or not.
                //But in practice ListView resets CurrentItem in some strange way.

                //So we check if CurrentItem does not belong to TableView anymore
                if (currentItem != null && rbTree.Find(currentItem) == null)
                {
                    InternalMoveCurrentTo(null);
                }

                RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset));

                //currentPosition = -1;

                //RaiseItemEvents();
            }
        }

        // returns true if specified flag in flags is set.
        bool CheckFlag(CollectionViewFlags flags)
        {
            return (Flags & flags) != 0;
        }

        void SetFlag(CollectionViewFlags flags, bool value)
        {
            if (value)
            {
                Flags = Flags | flags;
            }
            else
            {
                Flags = Flags & ~flags;
            }
        }

        void ClearRefreshFlags()
        {
            Flags &= ~RefreshMask;
        }

        bool IsEditingOrAddingNew
        {
            get
            {
                return IsAddingNew || IsEditingItem;
            }
        }

        bool IsRefreshNeeded
        {
            get
            {
                //return Flags.HasFlag(RefreshMask);

                return (Flags & RefreshMask) != 0;
            }
        }

        /// <summary>
        /// IsRefreshDeferred returns true if there
        /// is still an outstanding DeferRefresh in
        /// use.  If at all possible, derived classes
        /// should not call Refresh if IsRefreshDeferred
        /// is true.
        /// </summary>
        bool IsRefreshDeferred
        {
            get
            {
                return deferLevel > 0;
            }
        }

        // helper to validate that we are not in the middle of a DeferRefresh
        // and throw if that is the case.
        void VerifyRefreshNotDeferred()
        {
            // If the Refresh is being deferred to change filtering or sorting of the
            // data by this CollectionView, then CollectionView will not reflect the correct
            // state of the underlying data.

            if (IsRefreshDeferred)
            {
                throw new InvalidOperationException("CollectionView cannot reflect the changes if the refresh is being deferred.");
            }
        }

        /// <summary>
        /// Count of items without NewItemPlaceholder
        /// </summary>
        int InternalCount
        {
            get
            {
                return rbTree.Count;
            }
        }

        void CheckBeforeAddNew()
        {
            VerifyRefreshNotDeferred();

            if (!CanAddNew)
            {
                throw new InvalidOperationException("AddNew is not allowed.");
            }
        }

        void VerifyBeforeEndEdit()
        {
            if (IsAddingNew)
            {
                throw new InvalidOperationException(Format(MemberNotAllowedDuringTransaction, "CancelEdit", "AddNew"));
            }

            VerifyRefreshNotDeferred();

            if (editItem == null)
            {
                throw new InvalidOperationException("editItem is null during editing transaction.");
            }
        }

        int Index2Internal(int index)
        {
            if (newItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning)
            {
                return index - 1;
            }

            return index;
        }

        int Internal2Index(int internal_index)
        {
            if (newItemPlaceholderPosition == NewItemPlaceholderPosition.AtBeginning)
            {
                return internal_index + 1;
            }

            return internal_index;
        }

        public object GetItemAt(int index)
        {
            //throw new InvalidOperationException("This is a good exception! Just remove this line! This method is called at last!");

            switch (newItemPlaceholderPosition)
            {
                case NewItemPlaceholderPosition.None:

                    return rbTree.KeyByIndex(index);

                case NewItemPlaceholderPosition.AtBeginning:

                    return index == 0 ? NewItemPlaceholder : rbTree.KeyByIndex(index - 1);

                case NewItemPlaceholderPosition.AtEnd:

                    return index == InternalCount ? NewItemPlaceholder : rbTree.KeyByIndex(index);
            }

            throw new InvalidOperationException("Not a valid newItemPlaceholderPosition");
        }

        /// <summary>
        /// Position at wich the new item should be inserted.
        /// To remove new item use NewItemIndex - 1
        /// </summary>
        int NewItemIndex
        {
            get
            {
                int new_index = -1;

                switch (NewItemPlaceholderPosition)
                {
                    case NewItemPlaceholderPosition.None:

                        throw new InvalidOperationException("AddNew is not allowed if NewItemPlaceholderPosition is None.");

                    case NewItemPlaceholderPosition.AtBeginning:

                        new_index = 0;

                        break;

                    case NewItemPlaceholderPosition.AtEnd:

                        new_index = InternalCount;

                        break;
                }

                return new_index;
            }
        }

        object AddNewCommon(TView item)
        {
            throw new NotImplementedException();

            /*
            VerifyRefreshNotDeferred();
            
            if (item == NewItemPlaceholder)
            {
                throw new ArgumentException("Cannot Add Placeholder");
            }

            if (editItem != null)
            {
                throw new InvalidOperationException("Cannot call AddNew while active edit transaction.");
            }

            int new_index = NewItemIndex;

            rbTree.Insert(new_index, item);

            editItem = item;
            
            RaiseItemEvents(); //item, false

            editItem.BeginEdit(this);

            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                item,
                new_index));

            return item;
            */
        }

        void RaiseItemEvents()
        {
            //RaisePropertyChanged(() => CurrentPosition);
            RaisePropertyChanged(() => CurrentEditItem);
            RaisePropertyChanged(() => IsEditingItem);
            RaisePropertyChanged(() => CanCancelEdit);
            RaisePropertyChanged(() => CanAddNew);
            RaisePropertyChanged(() => CanAddNewItem);
            RaisePropertyChanged(() => CanRemove);
            RaisePropertyChanged(() => CurrentAddItem);
            RaisePropertyChanged(() => IsAddingNew);
            RaisePropertyChanged(() => CanRemove);
        }

        static string Format(string format, params object[] args)
        {
            return String.Format(format, args);
        }

        #endregion Helper methods

        #region Public methods that are called by DataGrid via Reflection

        /// <summary>
        /// Returns the index at which the specified item is located.
        /// Looks like DataGrid calls this method via Reflection
        /// </summary>
        /// <param name="item">The item to locate.</param>
        /// <returns>The index at which the specified item is located, or –1 
        /// if the item is unknown.</returns>
        public int IndexOf(object item)
        {
            if (item == NewItemPlaceholder)
            {
                return NewItemIndex;
            }

            int index = rbTree.IndexOfKey(Object2T(item));

            if (index != -1)
            {
                return Internal2Index(index);
            }

            return -1;
        }

        public int Count
        {
            get
            {
                int delta = newItemPlaceholderPosition != NewItemPlaceholderPosition.None ? 1 : 0;

                return rbTree.Count + delta;
            }
        }

        bool InternalMoveCurrentTo(TView item)
        {
            if (!object.ReferenceEquals(item, currentItem))
            {
                CurrentChangingEventArgs changing_args = new CurrentChangingEventArgs(true);

                RaiseCurrentChanging(changing_args);

                if (changing_args.Cancel)
                {
                    return false;
                }

                currentItem = item;

                RaisePropertyChanged(() => CurrentItem);
                RaisePropertyChanged(() => CurrentPosition);

                RaiseCurrentChanged();

                return true;
            }

            return false;
        }

        #endregion

        #region Private Data

        //null value means that TableView is unable to create new entities
        //and all the elements are added by AddNewView
        //ITableViewSite<TEntity> Site;
        
        /// <summary>
        /// It asks the table to add and remove the enities.
        /// And listens for change notifications.
        /// </summary>
        Table<TView> Table;

        Predicate<object> tableFilter = null;

        Predicate<string> propertyFilter = null;

        int deferLevel;

        //int currentPosition = -1;

        TView currentItem = null;

        CollectionViewFlags Flags = CollectionViewFlags.SourceCollectionChanged;

        SimpleMonitor _currentChangedMonitor = new SimpleMonitor();

        /// <summary>
        /// It does not relate to a collection of entities somehow.
        /// The entities could come from a table loaded from the database 
        /// or from some a list created by GUI code. 
        /// In the both cases we store only views.
        /// </summary>
        IOrderedTree<TView> rbTree;

        //I do not need this sourceCollection at all. The source collection is viewList.
        //System.Collections.IEnumerable sourceCollection;

        TView editItem; //should not be added to rbTree

        static object NewItemPlaceholder
        {
            get { return CollectionView.NewItemPlaceholder; }
        }

        private NewItemPlaceholderPosition newItemPlaceholderPosition = NewItemPlaceholderPosition.None;

        #endregion Private Data

        #region String Resources

        static string MemberNotAllowedDuringTransaction = "Member '{0}' is not allowed during '{1}' transaction.";

        #endregion String Resources

        #region Experimental

        public object AddNewManually(TView entity)
        {
            return AddNewItem(entity);
        }

        public object AddNewNoEdit(TView entity)
        {
            TView view = entity;

            AddView(view);

            return view;
        }

        public void InsertView(int index, TView item)
        {
            throw new NotImplementedException();
            
            /*
            VerifyRefreshNotDeferred();

            if (item == NewItemPlaceholder)
            {
                throw new ArgumentException("Cannot Add Placeholder");
            }

            rbTree.Insert(index, item);

            //editItem = item;

            //RaiseItemEvents(); //item, false

            //editItem.BeginEdit(this);

            RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                item,
                index));*/
        }

        public void AddView(TView item)
        {
            throw new NotImplementedException();

            //InsertView(NewItemIndex, item);
        }

        #endregion Experimental
    }
}
