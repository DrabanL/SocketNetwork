using SocketNetwork.Models;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace SocketNetwork.Default {
    public class BufferManager : ISocketHandler, IDisposable {
        private Queue<SocketAsyncEventArgs> _pool;
        private readonly object _accessLock = new object();

        public BufferManager() {
            _pool = new Queue<SocketAsyncEventArgs>();

            extendPool(100);
        }

        private void extendPool(int totalCount) {
            lock (_accessLock)
                while (_pool.Count < totalCount)
                    _pool.Enqueue(new SocketAsyncEventArgs());
        }

        SocketAsyncEventArgs ISocketHandler.GetSocketEvent() {
            if (_pool.Count == 0)
                extendPool(10);

            return _pool.Dequeue();
        }

        void ISocketHandler.ReturnSocketEvent(SocketAsyncEventArgs e) {
            if (disposedValue) {
                using (e) ;
                    return;
            }

            _pool.Enqueue(e);
        }

        public void Release() {
            foreach (var socketEvent in _pool)
                using (socketEvent) ;

            _pool.Clear();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects).
                    Release();
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
