using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Diagnostics;
using DataEngine;
using System.Collections.ObjectModel;

namespace DataViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            var logDispatcher = new LogDispatcher(this.Dispatcher,
                delegate(LoggerItem item)
                {
                    ShowMessage(item.Message, item.Level);
                });

            LoggerFactory.Writer = logDispatcher;

            InitializeComponent();

            loggerGrid.ItemsSource = logDispatcher.Items;

            Logger = LoggerFactory.CreateLogger("MainWindow");

            Logger.Print("The application has started.");

            SampleData.Instance.Targets.CurrentChanged += Targets_CurrentChanged;

            //PresentationTraceSources.SetTraceLevel(targetsView.ItemContainerGenerator, PresentationTraceLevel.High);
        }

        void Targets_CurrentChanged(object sender, EventArgs e)
        {
            positionTextBlock.Text = SampleData.Instance.Targets.CurrentPosition.ToString();
        }

        Logger Logger;

        void ShowMessage(string message, LogLevel level)
        {
            MessageBoxImage image = MessageBoxImage.Information;

            switch (level)
            {
                case LogLevel.Error:
                    image = MessageBoxImage.Error;
                    break;
                case LogLevel.Warning:
                    image = MessageBoxImage.Warning;
                    break;
                case LogLevel.Info:
                    image = MessageBoxImage.Information;
                    break;
                case LogLevel.Trace:
                    image = MessageBoxImage.Asterisk;
                    break;
                case LogLevel.Debug:
                case LogLevel.Ignore:
                    image = MessageBoxImage.None;
                    break;
            }

            MessageBox.Show(message, this.Title, MessageBoxButton.OK, image);
        }

        int idToAdd = SampleData.Instance.TargetTable.Count + 1;
        
        private void AddTargetButton_Click(object sender, RoutedEventArgs e)
        {
            Table<Target> targets = SampleData.Instance.TargetTable;

            int id = idToAdd++;
            
            targets.Add(
                    new Target(id) {
                        Name = String.Format("new-target-{0}", id)
                    });
        }

        private void DeleteTargetButton_Click(object sender, RoutedEventArgs e)
        {
            Table<Target> targets = SampleData.Instance.TargetTable;

            Target target = SampleData.Instance.Targets.CurrentItem as Target;

            if (target != null)
            {
                targets.Remove(target);
            }

            //targets.RemoveById(idToRemove);

            //++idToRemove;
        }

        private void DetachButton_Click(object sender, RoutedEventArgs e)
        {
            targetsView.ItemsSource = null;

            SampleData.Instance.Targets = null;

            GC.Collect();

            GC.WaitForPendingFinalizers();
        }

        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            CancelEdit();

            TableView<Target> targets = SampleData.Instance.Targets;

            using (var deferer = targets.DeferRefresh()) //to prevent multiple refreshes
            {
                ListSortDirection direction = ListSortDirection.Descending;

                if (targets.SortDescriptions.Count > 0)
                {
                    if (targets.SortDescriptions[0].Direction == ListSortDirection.Descending)
                    {
                        direction = ListSortDirection.Ascending;
                    }

                    targets.SortDescriptions.Clear();
                }

                targets.SortDescriptions.Add(new SortDescription("Id", direction));
            }
        }

        private void SortByCheckButton_Click(object sender, RoutedEventArgs e)
        {
            CancelEdit();

            TableView<Target> targets = SampleData.Instance.Targets;

            using (var deferer = targets.DeferRefresh()) //to prevent multiple refreshes
            {
                ListSortDirection direction = ListSortDirection.Descending;

                if (targets.SortDescriptions.Count > 0)
                {
                    if (targets.SortDescriptions[0].Direction == ListSortDirection.Descending)
                    {
                        direction = ListSortDirection.Ascending;
                    }

                    targets.SortDescriptions.Clear();
                }

                targets.SortDescriptions.Add(new SortDescription("IsChecked", direction));
                targets.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
            }
        }

        private void SortByNameButton_Click(object sender, RoutedEventArgs e)
        {
            CancelEdit();

            TableView<Target> targets = SampleData.Instance.Targets;

            using (var deferer = targets.DeferRefresh()) //to prevent multiple refreshes
            {
                ListSortDirection direction = ListSortDirection.Descending;

                if (targets.SortDescriptions.Count > 0)
                {
                    if (targets.SortDescriptions[0].Direction == ListSortDirection.Descending)
                    {
                        direction = ListSortDirection.Ascending;
                    }

                    targets.SortDescriptions.Clear();
                }

                targets.SortDescriptions.Add(new SortDescription("Name", direction));
                targets.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
            }
        }

        private void FilterByCheckButton_Click(object sender, RoutedEventArgs e)
        {
            CancelEdit();

            TableView<Target> targets = SampleData.Instance.Targets;

            using (var deferer = targets.DeferRefresh()) //to prevent multiple refreshes
            {
                targets.Filter = delegate(object item)
                {
                    Target target = item as Target;

                    return target.IsChecked == false;
                };

                targets.PropertyFilter = delegate(string property_name)
                {
                    return property_name == "IsChecked";
                };
            }
        }

        private void UncheckAllButton_Click(object sender, RoutedEventArgs e)
        {
            Table<Target> targets = SampleData.Instance.TargetTable;

            foreach(Target target in targets)
            {
                target.IsChecked = false;
            }
        }

        RandomAccessTable<Target> testColl;
        
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            targetsGrid.ItemsSource = null;
            targetsGrid.DataContext = null;

            //ObservableCollection<Target> coll = new ObservableCollection<Target>();

            RandomAccessTable<Target> coll = new RandomAccessTable<Target>();

            for (long id = 1; id <= 3; ++id)
            {
                coll.Add(
                        new Target(id)
                        {
                            Name = String.Format("new-target-{0}", id)
                        });
            }

            targetsGrid.ItemsSource = coll;

            testColl = coll;
        }

        private void Test2Button_Click(object sender, RoutedEventArgs e)
        {
            testColl[0].Name = "changed";

            int id = 100;
            
            testColl.Add(
                new Target(id)
                {
                    Name = String.Format("new-target-{0}", id)
                });
        }

        private void CancelEdit()
        {
            TableView<Target> targets = SampleData.Instance.Targets;

            //targets.CancelNew();
            if (targets.IsEditingItem)
            {
                targets.CancelEdit();
            }
        }
    }
}
