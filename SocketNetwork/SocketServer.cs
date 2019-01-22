using SocketNetwork.Internals;
using SocketNetwork.Models;
using System;
using System.Net;
using System.Net.Sockets;

namespace SocketNetwork {
    public class SocketServer : SocketBase {

        public IServerHandler ServerHandler;

        public SocketServer() : base() { }

        public SocketServer(SocketOptions socketOptions) : base(socketOptions) { }

        public void Start(int port, int backlog) {
            Socket.Bind(new IPEndPoint(IPAddress.Any, port));
            Socket.Listen(backlog);

            acceptAsync();
        }

        private void acceptAsync() {
            if (SocketHandler == null)
                throw new ArgumentNullException(nameof(SocketHandler));

            var socketEvent = SocketHandler.GetSocketEvent();
            socketEvent.Completed += SocketEvent_Completed;

            if (!Socket.AcceptAsync(socketEvent))
                SocketEvent_Completed(Socket, socketEvent);
        }

        public void Stop() {
            Socket.Close();
        }

        internal override void processServerAccept(SocketAsyncEventArgs e) {
            try {
                if (e.SocketError != SocketError.Success) {
                    ServerHandler?.ConnectionAcceptError(e);
                    return;
                }

                ServerHandler?.ConnectionAccepted(e.AcceptSocket);
            } finally {
                acceptAsync();
            }
        }
    }
}
