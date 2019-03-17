using SocketNetwork.Internals;
using SocketNetwork.Models;
using System;
using System.Net;
using System.Net.Sockets;

namespace SocketNetwork {
    /// <summary>
    /// Socket server implementation based on <see cref="SocketBase"/>.
    /// </summary>
    public class SocketServer : SocketBase {

        public IServerHandler ServerHandler;

        /// <summary>
        /// Initializes <see cref="Socket"/> with default options to prepare it for first use.
        /// </summary>
        public SocketServer() : base() { }

        /// <summary>
        /// Initializes <see cref="Socket"/> to prepare it for first use.
        /// </summary>
        public SocketServer(SocketOptions socketOptions) : base(socketOptions) { }

        /// <summary>
        /// Binds and starts a listener on <paramref name="port"/> with a limit of <paramref name="backlog"/> connections.
        /// </summary>
        public void Start(int port, int backlog) {
            // bind on IPAddress.Any to accpet connection from all interfaces
            Socket.Bind(new IPEndPoint(IPAddress.Any, port));

            Socket.Listen(backlog);

            acceptAsync();
        }

        /// <summary>
        /// Start accepting connections on <see cref="Socket"/>
        /// </summary>
        private void acceptAsync() {
            if (EventHandler == null)
                // EventHandler must be implemented to accuire accept event
                throw new ArgumentNullException(nameof(EventHandler));

            var socketEvent = EventHandler.GetSocketEvent();

            // register the callback
            socketEvent.Completed += SocketEvent_Completed;

            if (!Socket.AcceptAsync(socketEvent))
                // the operation completed synchronously probably due to some error, so invoke the callback immidietly to process the result
                SocketEvent_Completed(Socket, socketEvent);
        }

        /// <summary>
        /// Terminates the listener. (<see cref="SocketServer"/> cannot be reused afterwards)
        /// </summary>
        public virtual void Stop() {
            Socket.Close();
        }

        /// <summary>
        /// Implements the accept method to process incoming socket connections.
        /// </summary>
        internal override void processServerAccept(SocketAsyncEventArgs e) {
            if (e.SocketError == SocketError.OperationAborted)
                // socket closed
                return;

            try {
                if (e.SocketError != SocketError.Success) {
                    // invoke the client handler to process the error in the implementation
                    ServerHandler?.ConnectionAcceptError(e);
                    return;
                }

                // invoke the connection handler to process the socket connection in implementation
                ServerHandler?.ConnectionAccepted(e.AcceptSocket);
            } finally {

                // in any case, resume accept on socket server
                acceptAsync();
            }
        }
    }
}
