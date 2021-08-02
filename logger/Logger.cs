using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using log4net.Core;

namespace PrimitiveLogger
{
    public sealed class Logger : IDisposable
    {
        #region Private Static
        private const string MessageFormat = "{0} - Class: {1}, Line: {2}";
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static Lazy<Logger> _instance = new Lazy<Logger>();
        private static string _sourceFile;
        private static string _memberName;
        private static int _lineNumber;
        #endregion

        #region Private Fields
        private GenericSpooler<LogEntry> _logSpooler;
        #endregion

        #region Properties
        private GenericSpooler<LogEntry> LogSpooler
        {
            get
            {
                _logSpooler = _logSpooler ?? new GenericSpooler<LogEntry>(SendToLog4Net);
                return _logSpooler;
            }
        }
        #endregion

        #region Ctors and Dtors
        /// <summary>
        /// Default Ctor, Lazy requires parameterless Ctor
        /// </summary>
        public Logger()
        {

        }
        #endregion

        #region Singleton (Lazy for Thread-safe)
        /// <summary>
        /// Static instance of Logger. (this is used "inline")
        /// </summary>
        /// <param name="sourceFile">the sourcefile will be put here</param>
        /// <param name="memberName">the member that called the log will be put here</param>
        /// <param name="lineNumber">the source code line number will be put here</param>
        /// <returns>the Logger instance (threadsafe)</returns>
        public static Logger Instance([CallerFilePath] string sourceFile = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            _sourceFile = sourceFile;
            _memberName = memberName;
            _lineNumber = lineNumber;

            return _instance.Value;
        }
        #endregion

        #region Publics

        /// <summary>
        /// Log a debug message
        /// </summary>
        /// <param name="messageFormat">the message format</param>
        /// <param name="ex">the logging exception (optional)</param>
        /// <param name="formatArgs">the formatting objects (optional)</param>
        public void Debug(string messageFormat, Exception ex = null, params object[] formatArgs)
        {
            var message = string.Format(messageFormat, formatArgs);
            LogEntry logEntry = new LogEntry()
            {
                Message = string.Format(MessageFormat, message, _memberName, _lineNumber),
                Exception = ex,
                LogLevel = Level.Debug
            };

            LogSpooler.AddItem(logEntry);
        }

        /// <summary>
        /// Log an Info message
        /// </summary>
        /// <param name="messageFormat">the message format</param>
        /// <param name="ex">the logging exception (optional)</param>
        /// <param name="formatArgs">the formatting objects (optional)</param>
        public void Info(string messageFormat, Exception ex = null, params object[] formatArgs)
        {
            var message = string.Format(messageFormat, formatArgs);
            LogEntry logEntry = new LogEntry()
            {
                Message = string.Format(MessageFormat, message, _memberName, _lineNumber),
                Exception = ex,
                LogLevel = Level.Info
            };

            LogSpooler.AddItem(logEntry);
        }

        /// <summary>
        /// Log a Warn message
        /// </summary>
        /// <param name="messageFormat">the message format</param>
        /// <param name="ex">the logging exception (optional)</param>
        /// <param name="formatArgs">the formatting objects (optional)</param>
        public void Warn(string messageFormat, Exception ex = null, params object[] formatArgs)
        {
            var message = string.Format(messageFormat, formatArgs);
            LogEntry logEntry = new LogEntry()
            {
                Message = string.Format(MessageFormat, message, _memberName, _lineNumber),
                Exception = ex,
                LogLevel = Level.Warn
            };

            LogSpooler.AddItem(logEntry);
        }

        /// <summary>
        /// Log an Error message
        /// </summary>
        /// <param name="messageFormat">the message format</param>
        /// <param name="ex">the logging exception (optional)</param>
        /// <param name="formatArgs">the formatting objects (optional)</param>
        public void Error(string messageFormat, Exception ex = null, params object[] formatArgs)
        {
            var message = string.Format(messageFormat, formatArgs);
            LogEntry logEntry = new LogEntry()
            {
                Message = string.Format(MessageFormat, message, _memberName, _lineNumber),
                Exception = ex,
                LogLevel = Level.Error
            };

            LogSpooler.AddItem(logEntry);
        }

        /// <summary>
        /// Log a Fatal message
        /// </summary>
        /// <param name="messageFormat">the message format</param>
        /// <param name="ex">the logging exception (optional)</param>
        /// <param name="formatArgs">the formatting objects (optional)</param>
        public void Fatal(string messageFormat, Exception ex = null, params object[] formatArgs)
        {
            var message = string.Format(messageFormat, formatArgs);
            LogEntry logEntry = new LogEntry()
            {
                Message = string.Format(MessageFormat, message, _memberName, _lineNumber),
                Exception = ex,
                LogLevel = Level.Warn
            };

            LogSpooler.AddItem(logEntry);
        }
        #endregion

        #region Privates
        private void SendToLog4Net(LogEntry logEntry)
        {
            if (logEntry.LogLevel == Level.Debug)
            {
                log.Debug(logEntry.Message, logEntry.Exception);
            }
            else if (logEntry.LogLevel == Level.Info)
            {
                log.Info(logEntry.Message, logEntry.Exception);
            }
            else if (logEntry.LogLevel == Level.Warn)
            {
                log.Warn(logEntry.Message, logEntry.Exception);
            }
            else if (logEntry.LogLevel == Level.Error)
            {
                log.Error(logEntry.Message, logEntry.Exception);
            }
            else if (logEntry.LogLevel == Level.Fatal)
            {
                log.Fatal(logEntry.Message, logEntry.Exception);
            }
        }
        #endregion

        #region Inner Classes
        private class LogEntry
        {
            public Level LogLevel { get; set; }
            public string Message { get; set; }
            public Exception Exception { get; set; }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_logSpooler != null)
                    {
                        _logSpooler.Stop();
                        _logSpooler.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Logger() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
        #endregion
    }
}
