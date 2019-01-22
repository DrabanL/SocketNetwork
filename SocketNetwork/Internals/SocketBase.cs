using SocketNetwork.Models;
using System;
using System.Net.Sockets;

namespace SocketNetwork.Internals {
    public class SocketBase : IDisposable {

        public Socket Socket { get; private set; }
        public ISocketHandler SocketHandler;

        private readonly SocketOptions _socketOptions;

        /// <summary>
        /// AddressFamily = AddressFamily.InterNetwork,
        /// ProtocolType = ProtocolType.Tcp,
        /// SocketType = SocketType.Stream,
        /// BufferSize = 0x1024
        /// </summary>
        internal SocketBase() : this(new SocketOptions() {
            AddressFamily = AddressFamily.InterNetwork,
            ProtocolType = ProtocolType.Tcp,
            SocketType = SocketType.Stream,
        }) { }

        internal SocketBase(SocketOptions options) {
            _socketOptions = options;

            Socket = new Socket(_socketOptions.AddressFamily, _socketOptions.SocketType, _socketOptions.ProtocolType);
        }

        internal SocketBase(Socket socket) {
            Socket = socket;
        }

        internal void SocketEvent_Completed(object sender, SocketAsyncEventArgs e) {
            e.Completed -= SocketEvent_Completed;

            switch (e.LastOperation) {
                case SocketAsyncOperation.Connect:
                    processClientConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    if (!processClientReceive(e))
                        return;
                    break;
                case SocketAsyncOperation.Send:
                    if (!processClientSend(e))
                        return;
                    break;
                case SocketAsyncOperation.Disconnect:
                    processClientDisconnect(e);
                    break;
                case SocketAsyncOperation.Accept:
                    processServerAccept(e);
                    break;
            }

            SocketHandler?.ReturnSocketEvent(e);
        }

        internal virtual void processClientDisconnect(SocketAsyncEventArgs e) { }

        internal virtual bool processClientSend(SocketAsyncEventArgs e) => true;

        internal virtual bool processClientReceive(SocketAsyncEventArgs e) => true;

        internal virtual void processClientConnect(SocketAsyncEventArgs e) { }

        internal virtual void processServerAccept(SocketAsyncEventArgs e) { }

        #region IDisposable Support
        protected bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects).
                    using (Socket)
                        Socket.Close();
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
