using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataEngine
{
    public static class LoggerFactory
    {
        static LoggerContainer Container = new LoggerContainer(null);

        public static Logger CreateLogger(string category)
        {
            return Container.CreateLogger(category);
        }

        public static LogLevel Level
        {
            get
            {
                return Container.Level;
            }

            set
            {
                Container.Level = value;
            }
        }

        public static ILogWriter Writer
        {
            get
            {
                return Container.Writer;
            }

            set
            {
                Container.Writer = value;
            }
        }
    }
    
    class LoggerContainer
    {
        ILogWriter theWriter;

        //it could be a CompositeLogWriter
        public LoggerContainer(ILogWriter writer)
        {
            theWriter = writer;
        }

        public ILogWriter Writer
        {
            get
            {
                return theWriter;
            }

            set
            {
                lock (this)
                {
                    theWriter = value;

                    foreach (Logger logger in Loggers.Values)
                    {
                        logger.Writer = theWriter;
                    }
                }
            }
        }

        Dictionary<string, Logger> Loggers = new Dictionary<string, Logger>();

        LogLevel logLevel = LogLevel.Ignore; //show all by default

        public LogLevel Level
        {
            get
            {
                return logLevel;
            }

            set
            {
                lock (this)
                {
                    logLevel = value;

                    foreach (Logger logger in Loggers.Values)
                    {
                        logger.Level = logLevel;
                    }
                }
            }
        }
        
        public Logger CreateLogger(string category)
        {
            lock (this)
            {
                Logger logger;

                if (Loggers.TryGetValue(category, out logger))
                {
                    return logger;
                }

                logger = new Logger(Writer, category)
                {
                    Level = logLevel
                };

                Loggers.Add(category, logger);

                return logger;
            }
        }
    }
}
