using SocketNetwork.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace SocketNetwork.Default {
    /// <summary>
    /// Managed a pool of SocketAsyncEventArgs to be effeciently re-used.
    /// </summary>
    public class SocketEventManager : ISocketEventHandler, IDisposable {
        /// <summary>
        /// Internal queue that manages the available SocketAsyncEventArgs objects.
        /// </summary>
        private Queue<SocketAsyncEventArgs> _pool;

        /// <summary>
        /// Locker object used for making sure the pool is extended in a thread-safe way.
        /// </summary>
        private readonly object _accessLock = new object();

        /// <summary>
        /// The initial defined pool size.
        /// </summary>
        private int _basePoolSize;

        /// <summary>
        /// Initializes the SocketAsyncEventArgs pool.
        /// </summary>
        /// <param name="poolSize"></param>
        public SocketEventManager(int poolSize = 100) {
            _basePoolSize = poolSize;
            _pool = new Queue<SocketAsyncEventArgs>();

            // set the pool to initial desired size
            extendPool(poolSize);
        }

        /// <summary>
        /// Extends the pool's total objects count.
        /// </summary>
        /// <param name="totalCount"></param>
        private void extendPool(int totalCount) {
            lock (_accessLock)
                // lock this operation to be sure that only one thread modifies the queue at any given time.
                while (_pool.Count < totalCount)
                    // extend the pool until desired size is reached
                    _pool.Enqueue(new SocketAsyncEventArgs());
        }

        SocketAsyncEventArgs ISocketEventHandler.GetSocketEvent() {
            if (_pool.Count == 0)
                extendPool((int)(Math.Round(_basePoolSize * 0.1))); // extend the pool by 10%

            // return SocketAsyncEventArgs object from the pool.
            return _pool.Dequeue();
        }

        void ISocketEventHandler.ReturnSocketEvent(SocketAsyncEventArgs e) {
            if (disposedValue) {
                // the class have been destroyed, so cleanup and return
                using (e) ;
                    return;
            }

            // return the SocketAsyncEventArgs to the pool for re-use.
            _pool.Enqueue(e);
        }

        /// <summary>
        /// Cleans up locally managed objects.
        /// </summary>
        private void disposeManagedObjects() {
            try {
                // dispose all the pool objects
                foreach (var socketEvent in _pool)
                    using (socketEvent) ;

                _pool.Clear();
            } catch {
                // catch all errors because we cannot break flow when being called from Dispose method.
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects).
                    disposeManagedObjects();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~BufferManager() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
