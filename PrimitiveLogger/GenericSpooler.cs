using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace PrimitiveLogger
{
    /// <summary>
    /// An asynchronous generic executing class that controls spooling -- 
    /// putting jobs on a queue and taking them off one at a time. This 
    /// class will not block the adding of items in the spool.  And will 
    /// execute the delegate in a thread-safe manner.
    /// </summary>
    /// <remarks>
    /// There is ONLY one thread that executes the Action delegate.  It
    /// is assumed that the Action delegate blocks. Thus the spooler will 
    /// ONLY execute each item as fast as the Action delegate executes on one
    /// item.
    /// </remarks>
    /// <typeparam name="T">The Type of object to provide spooling for</typeparam>
    public class GenericSpooler<T> : IGenericSpooler<T>
    {
        #region Class Scope Members
        bool _isDisposed;

        /// <summary>
        /// Threadsafe collection class
        /// </summary>
        readonly ConcurrentQueue<ItemMetaData> _inputs;

        /// <summary>
        /// The worker thread that supplies each item to the callback
        /// </summary>
        Thread _processWorkerThread;

        /// <summary>
        /// Wait handles to control process flow
        /// </summary>
        readonly WaitHandle[] _operationHandles;

        /// <summary>
        /// Traffic stop/go control event
        /// </summary>
        AutoResetEvent _trafficEvent;

        /// <summary>
        /// Signal this event to stop the spooler and exit the process thread
        /// </summary>
        AutoResetEvent _exitEvent;

        /// <summary>
        /// Pause/Resume control event
        /// </summary>
        ManualResetEvent _itemActionEvent;

        #endregion

        #region Properties
        /// <summary>
        /// True if there are Currently more items in the queue
        /// </summary>
        public bool HasMore
        {
            get
            {
                return _inputs.Count > 0;
            }
        }
        #endregion

        #region Delegates and Events
        /// <summary>
        /// Delegate to use when getting notification that an exception has occurred
        /// </summary>
        /// <param name="sender">could be either the spooler or the object containing the callback (the callback produced the exception)</param>
        /// <param name="ex">the exception caught</param>
        public delegate void ExceptionEncounteredDelegate(object sender, Exception ex);

        /// <summary>
        /// Get notification that an exception has occurred
        /// </summary>
        public event ExceptionEncounteredDelegate ExceptionEncountered;

        public delegate void SpoolerEmptyDelegate();

        public event SpoolerEmptyDelegate SpoolerEmpty;

        /// <summary>
        /// The delegate to use for the callback
        /// </summary>
        private readonly Action<T> _spoolerAction;
        #endregion

        #region Ctors and Dtors
        /// <summary>
        /// Default Ctor.  The callback is required.
        /// </summary>
        /// <param name="spoolerAction">the delegate to receive each item from the spool</param>
        public GenericSpooler(Action<T> spoolerAction)
        {
            if (spoolerAction == null)
            {
                throw new NullReferenceException("Spooler Action cannot be null");
            }

            _inputs = new ConcurrentQueue<ItemMetaData>();
            _spoolerAction = spoolerAction;

            _trafficEvent = new AutoResetEvent(false);
            _exitEvent = new AutoResetEvent(false);
            _itemActionEvent = new ManualResetEvent(true);

            _operationHandles = new WaitHandle[2];
            _operationHandles[0] = _trafficEvent;
            _operationHandles[1] = _exitEvent;
        }

        /// <summary>
        /// Finalizer . . . 
        /// </summary>
        ~GenericSpooler()
        {
            Dispose(false);
        }

        #endregion

        #region Public
        /// <summary>
        /// Adds an Item of type T to the queue
        /// </summary>
        /// <param name="item">the item of type T</param>
        /// <param name="itemCausesStop">if true the spooler stops after this item and must be restarted</param>
        public void AddItem(T item, bool itemCausesStop = false)
        {
            try
            {
                ItemMetaData storedItem = new ItemMetaData()
                {
                    HoldOnItem = itemCausesStop,
                    Item = item
                };

                _inputs.Enqueue(storedItem);
                StartProcess();
                _trafficEvent.Set();
            }
            catch (Exception ex)
            {
                NotifyException(this, ex);
            }
        }

        /// <summary>
        /// Stops the spooler and empties the Queue
        /// </summary>
        public void Reset()
        {
            try
            {
                _itemActionEvent.Reset();

                ItemMetaData ignoredData;
                while (_inputs.TryDequeue(out ignoredData))
                {
                    // do nothing; just clear the queue
                }

                _itemActionEvent.Set();
            }
            catch (Exception ex)
            {
                NotifyException(this, ex);
            }
        }

        /// <summary>
        /// Stop spooling items.  Items can still be added/removed/replaced but will not be spooled out
        /// </summary>
        public void Stop()
        {
            try
            {
                _itemActionEvent.Reset();
            }
            catch (Exception ex)
            {
                NotifyException(this, ex);
            }
        }

        /// <summary>
        /// Starts the spooling again.  All items still in the queue are sent out.
        /// </summary>
        public void Resume()
        {
            try
            {
                _itemActionEvent.Set();
            }
            catch (Exception ex)
            {
                NotifyException(this, ex);
            }
        }
        #endregion

        #region Virtuals
        /// <summary>
        /// Virtual method to override for adding general disposal by extending classes
        /// </summary>
        public virtual void GeneralDispose()
        {

        }

        /// <summary>
        /// Virtual method to override for adding dispose called using the Dispose() by extending classes
        /// </summary>
        public virtual void DeterministicDispose()
        {

        }
        /// <summary>
        /// Virtual method to override for adding Finalizer disposal by calling the Destructor (finalize) on extending classes
        /// </summary>
        public virtual void FinalizeDispose()
        {

        }
        #endregion

        #region Privates
        /// <summary>
        /// Starts the thread that spools the items off the queue (if not yet started)
        /// </summary>
        private void StartProcess()
        {
            if (_processWorkerThread == null)
            {
                _processWorkerThread = new Thread(ProcessWhileHasInput);
                _processWorkerThread.Start();
            }
        }

        /// <summary>
        /// The method the thread executes.  The thread executes the Callback delegate for each item it takes off the queue.
        /// </summary>
        private void ProcessWhileHasInput()
        {
            try
            {
                // Keep the thread alive unless the exit event is signaled
                while (true)
                {
                    int iWaitEvent = WaitHandle.WaitAny(_operationHandles);

                    if (_operationHandles[iWaitEvent] == _trafficEvent)
                    {
                        ItemMetaData itemData;
                        while (_inputs.TryDequeue(out itemData))
                        {
                            // allow stop/resume using itemActionEvent signaling
                            if (_itemActionEvent.WaitOne())
                            {
                                try
                                {
                                    if (itemData.HoldOnItem)
                                        _itemActionEvent.Reset();

                                    _spoolerAction(itemData.Item);
                                }
                                catch (Exception ex)
                                {
                                    NotifyException(_spoolerAction.Target, ex);
                                }
                            }
                        }

                        var spoolerEvent = SpoolerEmpty;
                        if (spoolerEvent != null)
                        {
                            spoolerEvent();
                        }
                    }
                    else if (_operationHandles[iWaitEvent] == _exitEvent)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // Normal call to abort for the thread, call in finalize will necessarily end here if 
                // the thread has not already been stopped
                NotifyException(this, ex);
            }
        }

        /// <summary>
        /// Call the event (if there are any listeners)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ex"></param>
        private void NotifyException(object sender, Exception ex)
        {
            if (ExceptionEncountered != null)
            {
                ExceptionEncountered(sender, ex);
            }
        }

        #endregion

        #region IDisposable Members
        /// <summary>
        /// Dispose implementation to type of disposing
        /// </summary>
        /// <param name="disposing">True for deterministic, false for finalization</param>
        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;

            // General cleanup logic here
            GeneralDispose();

            // if the process is paused, release it
            _itemActionEvent.Set();

            // Signal the thread to end and exit
            _exitEvent.Set();

            if (disposing) // Deterministic only cleanup
            {
                DeterministicDispose();
            }
            else // Finalizer only cleanup
            {
                FinalizeDispose();

                // if the worker thread is still running on finalize, abort it!
                if (_processWorkerThread != null)
                    _processWorkerThread.Abort();

                _processWorkerThread = null;
            }
            _isDisposed = true;
        }

        /// <summary>
        /// Release all resources (clears List)
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Inner classes
        private class ItemMetaData
        {
            /// <summary>
            /// If true spooler stops after this item
            /// </summary>
            public bool HoldOnItem { get; set; }

            /// <summary>
            /// The item in the spool
            /// </summary>
            public T Item { get; set; }
        }
        #endregion
    }
}
