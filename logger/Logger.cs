using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using log4net.Core;

namespace PrimitiveLogger
{
    public sealed class Logger : IDisposable
    {
        #region Private Static
        private const string MessageFormat = "{0} - File: {1}, Method: {2}, Line: {3}";
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static Lazy<Logger> _instance = new Lazy<Logger>();
        private static string _sourceFile;
        private static string _methodName;
        private static int _lineNumber;

        private Action<string> _infoHandler;
        private Action<string> _warningHandler;
        private Action<object> _errorHandler;

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
        /// <param name="methodName">the member that called the log will be put here</param>
        /// <param name="lineNumber">the source code line number will be put here</param>
        /// <returns>the Logger instance (threadsafe)</returns>
        public static Logger Instance([CallerFilePath] string sourceFile = "", [CallerMemberName] string methodName = "", [CallerLineNumber] int lineNumber = 0)
        {
            _sourceFile = sourceFile;
            _methodName = methodName;
            _lineNumber = lineNumber;

            return _instance.Value;
        }
        #endregion

        #region Publics

        /// <summary>
        /// Log a debug message
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="ex">the logging exception (optional)</param>
        /// <param name="formatArgs">the formatting objects (optional)</param>
        public void Debug(string message, Exception ex = null, params object[] formatArgs)
        {
            _infoHandler?.Invoke(message);

            LogEntry logEntry = new LogEntry()
            {
                Message = string.Format(message, formatArgs),
                Exception = ex,
                LogLevel = Level.Debug,
                MethodName = _methodName,
                FilePath = _sourceFile,
                LineNumber = _lineNumber
            };

            LogSpooler.AddItem(logEntry);
        }

        /// <summary>
        /// Log an Info message
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="ex">the logging exception (optional)</param>
        /// <param name="formatArgs">the formatting objects (optional)</param>
        public void Info(string message, Exception ex = null, params object[] formatArgs)
        {
            _infoHandler?.Invoke(message);

            LogEntry logEntry = new LogEntry()
            {
                Message = string.Format(message, formatArgs),
                Exception = ex,
                LogLevel = Level.Info,
                MethodName = _methodName,
                FilePath = _sourceFile,
                LineNumber = _lineNumber
            };

            LogSpooler.AddItem(logEntry);
        }

        /// <summary>
        /// Log a Warn message
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="ex">the logging exception (optional)</param>
        /// <param name="formatArgs">the formatting objects (optional)</param>
        public void Warn(string message, Exception ex = null, params object[] formatArgs)
        {
            _warningHandler?.Invoke(message);

            LogEntry logEntry = new LogEntry()
            {
                Message = string.Format(message, formatArgs),
                Exception = ex,
                LogLevel = Level.Warn,
                MethodName = _methodName,
                FilePath = _sourceFile,
                LineNumber = _lineNumber
            };

            LogSpooler.AddItem(logEntry);
        }

        /// <summary>
        /// Log an Error message
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="ex">the logging exception (optional)</param>
        /// <param name="formatArgs">the formatting objects (optional)</param>
        public void Error(string message, Exception ex = null, params object[] formatArgs)
        {
            _errorHandler?.Invoke($"{message} : {ex}");

            LogEntry logEntry = new LogEntry()
            {
                Message = string.Format(message, formatArgs),
                Exception = ex,
                LogLevel = Level.Error,
                MethodName = _methodName,
                FilePath = _sourceFile,
                LineNumber = _lineNumber
            };

            LogSpooler.AddItem(logEntry);
        }

        /// <summary>
        /// Log a Fatal message
        /// </summary>
        /// <param name="message">message</param>
        /// <param name="ex">the logging exception (optional)</param>
        /// <param name="formatArgs">the formatting objects (optional)</param>
        public void Fatal(string message, Exception ex = null, params object[] formatArgs)
        {
            _errorHandler?.Invoke($"{message} : {ex}");

            LogEntry logEntry = new LogEntry()
            {
                Message = string.Format(message, formatArgs),
                Exception = ex,
                LogLevel = Level.Warn,
                MethodName = _methodName,
                FilePath = _sourceFile,
                LineNumber = _lineNumber
            };

            LogSpooler.AddItem(logEntry);
        }
        #endregion

        #region Privates
        private void SendToLog4Net(LogEntry logEntry)
        {
            string message = string.Format(MessageFormat, logEntry.Message, Path.GetFileName(logEntry.FilePath),
                logEntry.MethodName, _lineNumber);

            if (logEntry.LogLevel == Level.Debug)
            {
                log.Debug(message, logEntry.Exception);
            }
            else if (logEntry.LogLevel == Level.Info)
            {
                log.Info(message, logEntry.Exception);
            }
            else if (logEntry.LogLevel == Level.Warn)
            {
                log.Warn(message, logEntry.Exception);
            }
            else if (logEntry.LogLevel == Level.Error)
            {
                log.Error(message, logEntry.Exception);
            }
            else if (logEntry.LogLevel == Level.Fatal)
            {
                log.Fatal(message, logEntry.Exception);
            }
        }
        #endregion

        #region Inner Classes
        private class LogEntry
        {
            public Level LogLevel { get; set; }
            public string Message { get; set; }
            public Exception Exception { get; set; }
            public string FilePath { get; set; }
            public int LineNumber { get; set; }
            public string MethodName { get; set; }
        }

        public void SetUnityLogHandlers(Action<string> infoHandler, Action<string> warnHandler, Action<object> errorHandler)
        {
            _infoHandler = infoHandler;
            _warningHandler = warnHandler;
            _errorHandler = errorHandler;
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
