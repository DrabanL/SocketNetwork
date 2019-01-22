using SocketNetwork.Internals;
using SocketNetwork.Models;
using System;
using System.Net;
using System.Net.Sockets;

namespace SocketNetwork {
    public abstract class SocketClient : SocketBase {

        public IClientHandler ClientHandler;
        public INetworkMessageSerializationHandler SerializationHandler;

        public SocketClient() : base() { }

        public SocketClient(Socket socket) : base(socket) { }

        public SocketClient(SocketOptions socketOptions) : base(socketOptions) { }

        public void ConnectAsync(string host, int port) {
            if (SocketHandler == null)
                throw new ArgumentNullException(nameof(SocketHandler));

            var socketEvent = SocketHandler.GetSocketEvent();
            socketEvent.Completed += SocketEvent_Completed;
            socketEvent.RemoteEndPoint = new DnsEndPoint(host, port);

            if (!Socket.ConnectAsync(socketEvent))
                SocketEvent_Completed(Socket, socketEvent);
        }

        internal override void processClientConnect(SocketAsyncEventArgs e) {
            if (e.SocketError != SocketError.Success) {
                ClientHandler?.ConnectionError(this, e);
                return;
            }

            ClientHandler?.ConnectionOpened(this);
        }

        internal override bool processClientReceive(SocketAsyncEventArgs e) {
            switch (e.SocketError) {
                case SocketError.ConnectionReset:
                    ClientHandler?.ConnectionClosed(this);
                    return true;
            }

            if (e.SocketError != SocketError.Success) {
                ClientHandler?.ConnectionError(this, e);
                return true;
            }

            var messageHandler = e.UserToken as NetworkMessageHandler;
            if (messageHandler.CompleteReceive(e.BytesTransferred)) {
                ClientHandler?.ConnectionMessage(this, SerializationHandler.Deserialize(messageHandler.GetFinalized()));
                messageHandler.Reset();
            }

            receiveAsync(e);

            return false;
        }

        internal override bool processClientSend(SocketAsyncEventArgs e) {
            if (e.SocketError != SocketError.Success) {
                ClientHandler?.ConnectionError(this, e);
                return true;
            }

            var messageHandler = e.UserToken as NetworkMessageHandler;
            if (!messageHandler.CompleteSend(e.BytesTransferred)) {
                e.Completed += SocketEvent_Completed;
                e.SetBuffer(messageHandler.Buffer, messageHandler.Offset, messageHandler.Length);

                if (!Socket.SendAsync(e))
                    SocketEvent_Completed(Socket, e);

                return false;
            }

            return true;
        }

        internal override void processClientDisconnect(SocketAsyncEventArgs e) {
            if (e.SocketError != SocketError.Success) {
                ClientHandler?.ConnectionError(this, e);
                return;
            }

            ClientHandler?.ConnectionClosed(this);
        }

        public void ReceiveAsync<T>() where T : NetworkMessageHandler {
            if (SocketHandler == null)
                throw new ArgumentNullException(nameof(SocketHandler));

            var socketEvent = SocketHandler.GetSocketEvent();
            socketEvent.UserToken = Activator.CreateInstance<T>();

            receiveAsync(socketEvent);
        }

        private void receiveAsync(SocketAsyncEventArgs socketEvent) {
            var messageHandler = socketEvent.UserToken as NetworkMessageHandler;

            socketEvent.Completed += SocketEvent_Completed;
            socketEvent.SetBuffer(
                messageHandler.Buffer,
                messageHandler.Offset,
                messageHandler.Length);

            if (!Socket.ReceiveAsync(socketEvent))
                SocketEvent_Completed(Socket, socketEvent);
        }


        public void SendAsync<T>(T message) where T : NetworkMessageHandler {
            if (SocketHandler == null)
                throw new ArgumentNullException(nameof(SocketHandler));

            message.SetFinalized(SerializationHandler.Serialize(message));

            var socketEvent = SocketHandler.GetSocketEvent();
            socketEvent.UserToken = message;

            socketEvent.Completed += SocketEvent_Completed;
            socketEvent.SetBuffer(message.Buffer, message.Offset, message.Length);

            if (!Socket.SendAsync(socketEvent))
                SocketEvent_Completed(Socket, socketEvent);
        }

        public void DisconnectAsync() {
            if (SocketHandler == null)
                throw new ArgumentNullException(nameof(SocketHandler));

            var socketEvent = SocketHandler.GetSocketEvent();
            socketEvent.Completed += SocketEvent_Completed;

            if (!Socket.DisconnectAsync(socketEvent))
                SocketEvent_Completed(Socket, socketEvent);
        }
    }
}
