using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Windows.Threading;
using System.Diagnostics;

namespace DataEngine
{
    /// <summary>
    /// Error means that the execution of some task could not be completed; 
    /// an email couldn't be sent, a page couldn't be rendered, some data 
    /// couldn't be stored to a database, something like that. Something has 
    /// definitively gone wrong.
    /// Warning means that something unexpected happened, but that execution 
    /// can continue, perhaps in a degraded mode; a configuration file was 
    /// missing but defaults were used, a price was calculated as negative, 
    /// so it was clamped to zero, etc. Something is not right, but it hasn't 
    /// gone properly wrong yet - warnings are often a sign that there will 
    /// be an error very soon.
    /// Info means that something normal but significant happened; the system 
    /// started, the system stopped, the daily inventory update job ran, etc. 
    /// There shouldn't be a continual torrent of these, otherwise there's just 
    /// too much to read.
    /// Debug means that something normal and insignificant happened; a new 
    /// user came to the site, a page was rendered, an order was taken, a price 
    /// was updated. This is the stuff excluded from info because there would be 
    /// too much of it.
    /// trace: we don't use this often, but this would be for extremely detailed 
    /// and potentially high volume logs that you don't typically want enabled 
    /// even during normal development. Examples include dumping a full object 
    /// hierarchy, logging some state during every iteration of a large loop, etc.
    /// </summary>
    public enum LogLevel
    {
        Ignore, //messages are never shown
        Trace,
        Debug,
        Info,
        Warning,
        Error
    }
    
    public class LoggerItem : NotificationObject
    {
        public LogLevel Level { get; private set; }

        public LoggerItem(LogLevel level)
        {
            Modal = false;

            Level = level;
        }

        DateTime theDate = DateTime.Now;

        public bool Modal { get; set; }

        public DateTime Date
        {
            get { return theDate; }
        }

        string theCategory;

        public string Category
        {
            get { return theCategory; }

            set
            {
                if (value != theCategory)
                {
                    theCategory = value;

                    RaisePropertyChanged(() => Category);
                }
            }
        }

        string theMessage;

        public string Message
        {
            get { return theMessage; }

            set
            {
                if (value != theMessage)
                {
                    theMessage = value;

                    RaisePropertyChanged(() => Message);
                }
            }
        }

        public string Details { get; set; }
    }

    public interface ILogWriter
    {
        void Write(LoggerItem item);
    }

    //I did not used this yet
    public class CompositeLogWriter : ILogWriter
    {
        List<ILogWriter> Writers = new List<ILogWriter>();

        public void AddWriter(ILogWriter w)
        {
            lock (this)
            {
                Writers.Add(w);
            }
        }

        public void RemoveWriter(ILogWriter w)
        {
            lock (this)
            {
                Writers.Remove(w);
            }
        }
        
        public void Write(LoggerItem item)
        {
            ILogWriter[] writers;
            
            lock (this)
            {
                writers = Writers.ToArray();
            }
            
            foreach (ILogWriter writer in writers)
            {
                writer.Write(item);
            }
        }
    }

    public class FakeLogWriter : ILogWriter
    {
        public void Write(LoggerItem item)
        {
            //it does nothing
        }
    }

    public class Logger
    {
        //Logger could be used without the factory
        public ILogWriter Writer { get; set; }

        //it will not show messages with lower level
        public LogLevel Level { get; set; }

        bool CheckLevel(LogLevel level)
        {
            return level >= Level;
        }

        readonly string Category;

        public Logger(string category) 
            : this(new FakeLogWriter(), category)
        {
        }

        public Logger(ILogWriter writer)
            : this(writer, "")
        {
        }

        public Logger(ILogWriter writer, string category)
        {
            Category = category;

            Writer = writer;
        }

        public void Print(LogLevel level, string format, params object[] args)
        {
            if (CheckLevel(level))
            {
                LoggerItem item = new LoggerItem(level)
                {
                    Message = String.Format(format, args),
                    Category = this.Category
                };
                
                //System.Diagnostics.Debug.Print(item.Message);

                Writer.Write(item);
            }
        }

        public void Print(string format, params object[] args)
        {
            Print(LogLevel.Debug, format, args);
        }

        [Conditional("DEBUG")]
        public void Trace(LogLevel level, string format, params object[] args)
        {
            Print(level, format, args);
        }

        [Conditional("DEBUG")]
        public void Trace(string format, params object[] args)
        {
            Print(format, args);
        }
        
        public void Show(LogLevel level, string format, params object[] args)
        {
            if (CheckLevel(level))
            {
                LoggerItem item = new LoggerItem(level)
                {
                    Modal = true,
                    Message = String.Format(format, args),
                    Category = this.Category
                };

                Writer.Write(item);
            }
        }

        void ReportException(string message, Exception x, bool modal)
        {
            LoggerItem item = new LoggerItem(LogLevel.Error)
            {
                Modal = modal,
                
                Message = message,

                Category = this.Category,

                Details = Format.GetExceptionMessage(x)
            };

            Writer.Write(item);
        }

        public void PrintException(Exception x, string format, params object[] args)
        {
            ReportException(String.Format(format, args), x, false);
        }
        
        public void PrintException(Exception x)
        {
            ReportException(x.Message, x, false);
        }

        public void ShowException(Exception x, string format, params object[] args)
        {
            ReportException(String.Format(format, args), x, true);
        }

        public void ShowException(Exception x)
        {
            ReportException(x.Message, x, true);
        }
    }

    public delegate void LoggerItemDelegate(LoggerItem item);

    public class LogDispatcher : ILogWriter
    {
        BindingList<LoggerItem> loggerItems = new BindingList<LoggerItem>();

        Dispatcher Dispatcher;

        LoggerItemDelegate ShowMessage;

        public LogDispatcher(Dispatcher dispatcher, LoggerItemDelegate show_message)
        {
            Dispatcher = dispatcher;

            ShowMessage = show_message;

            LogLimit = 50;
        }

        public int LogLimit { get; set; }

        public BindingList<LoggerItem> Items { get { return loggerItems; } }

        void AddLoggerItem(LoggerItem item)
        {
            if (loggerItems.Count > LogLimit)
            {
                //loggerItems.RemoveAt(loggerItems.Count - 1);

                int tail_len = loggerItems.Count - LogLimit;

                for (int i = 0; i < tail_len; ++i)
                {
                    loggerItems.RemoveAt(LogLimit);
                }
            }

            loggerItems.Insert(0, item);

            if (item.Modal)
            {
                ShowMessage(item);
            }
        }

        public void Write(LoggerItem item)
        {
            if (this.Dispatcher.CheckAccess())
            {
                // The calling thread owns the dispatcher, and hence the UI element

                AddLoggerItem(item);
            }
            else
            {
                //invocation required
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,
                    new LoggerItemDelegate(AddLoggerItem), item);
            }
        }
    }

    public class ConsoleLogWriter : ILogWriter
    {
        public void Write(LoggerItem item)
        {
            //remove the BELL symbol (according to ASCII) to stop the Windows console 
            //triggers beeps when displaying binary data
            
            String message = item.Message.Replace('\x7', ' ');

            Console.WriteLine("{0}:{1}", item.Date, message);
        }
    }
}
