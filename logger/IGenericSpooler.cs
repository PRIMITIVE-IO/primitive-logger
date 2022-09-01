using System;
using JetBrains.Annotations;

namespace PrimitiveLogger
{
    [PublicAPI]
    public interface IGenericSpooler<T> : IDisposable
    {
        /// <summary>
        /// Add Item to the spool
        /// </summary>
        /// <param name="item"></param>
        void AddItem(T item, bool itemCausesStop = false);

        /// <summary>
        /// Stop spooling for now
        /// </summary>
        void Stop();

        /// <summary>
        /// Resume spooling after being stopped
        /// </summary>
        void Resume();

        /// <summary>
        /// Some exception happened in the spool, either in the spooler or in the message handler
        /// </summary>
        event GenericSpooler<T>.ExceptionEncounteredDelegate ExceptionEncountered;
    }
}
