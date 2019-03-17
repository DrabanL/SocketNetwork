using SocketNetwork.Default;
using SocketNetwork.Models;
using System;
using System.Net.Sockets;

namespace SocketNetwork.Internals {
    /// <summary>
    /// A base socket class that represent the foundation of either a Client or a Server and host any objects and implementation relevent to both functions.
    /// </summary>
    public class SocketBase : IDisposable {

        /// <summary>
        /// The base socket object.
        /// </summary>
        public Socket Socket { get; private set; }

        public ISocketEventHandler EventHandler;

        private readonly SocketOptions _socketOptions;

        /// <summary>
        /// Initializes <see cref="Socket"/> with default options to prepare it for first use.
        /// </summary>
        internal SocketBase() : this(SocketOptions.Default) { }

        /// <summary>
        /// Initializes <see cref="Socket"/> to prepare it for first use.
        /// </summary>
        internal SocketBase(SocketOptions options) {
            _socketOptions = options;

            Socket = new Socket(_socketOptions.AddressFamily, _socketOptions.SocketType, _socketOptions.ProtocolType);
        }

        /// <summary>
        /// Initializes <see cref="Socket"/> socket with an existing object.
        /// Usually should be used when the Socket object has been managed externally (for example once Server receives a Socket connection).
        /// </summary>
        internal SocketBase(Socket socket) {
            Socket = socket;
        }

        /// <summary>
        /// A Callback for handling the socket Async events.
        /// </summary>
        internal void SocketEvent_Completed(object sender, SocketAsyncEventArgs e) {
            // Unsubscribe the callback because its being subscribed to on every socket event
            e.Completed -= SocketEvent_Completed;

            switch (e.LastOperation) {
                case SocketAsyncOperation.Connect:
                    processClientConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    if (!processClientReceive(e))
                        // there is still data to be processed, the receive operation on the event object is not done yet
                        return;
                    break;
                case SocketAsyncOperation.Send:
                    if (!processClientSend(e))
                        // there is still data to be processed, the send operation on the event object is not done yet
                        return;
                    break;
                case SocketAsyncOperation.Accept:
                    processServerAccept(e);
                    break;
            }

            // The communication operation completed, so free up the event object
            EventHandler.ReturnSocketEvent(e);
        }

        /// <summary>
        /// Handles the SocketAsyncOperation.Send event. Returns true if the socket event can be freed up.
        /// </summary>
        internal virtual bool processClientSend(SocketAsyncEventArgs e) => true;

        /// <summary>
        /// Handles the SocketAsyncOperation.Receive event. Returns true if the socket event can be freed up.
        /// </summary>
        internal virtual bool processClientReceive(SocketAsyncEventArgs e) => true;

        /// <summary>
        /// Handles the SocketAsyncOperation.Connect event.
        /// </summary>
        internal virtual void processClientConnect(SocketAsyncEventArgs e) { }

        /// <summary>
        /// Handles the SocketAsyncOperation.Accept event.
        /// </summary>
        internal virtual void processServerAccept(SocketAsyncEventArgs e) { }

        /// <summary>
        /// Cleans up locally managed objects.
        /// </summary>
        internal virtual void disposeManagedObjects() {
            try {
                using (Socket)
                    // Close the socket if its not already closed
                    Socket?.Close();
            } catch {
                // catch all errors because we cannot break flow when being called from Dispose method.
            }
        }

        #region IDisposable Support
        protected bool disposedValue = false; // To detect redundant calls

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
        // ~SocketBase() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
