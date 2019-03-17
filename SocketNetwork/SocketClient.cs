using SocketNetwork.Internals;
using SocketNetwork.Models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketNetwork {
    /// <summary>
    /// Socket client implementation based on <see cref="SocketBase"/>.
    /// </summary>
    public abstract class SocketClient : SocketBase {

        public IClientHandler ClientHandler;
        public INetworkMessageSerializationHandler SerializationHandler;

        /// <summary>
        /// Wait handle that resets when Connect task is completed.
        /// </summary>
        public readonly ManualResetEvent ConnectTaskCompleteSignal = new ManualResetEvent(false);

        /// <summary>
        /// Initializes <see cref="Socket"/> with default options to prepare it for first use.
        /// </summary>
        public SocketClient() : base() { }

        /// <summary>
        /// Initializes <see cref="Socket"/> with an existing object.
        /// Usually should be used when the <see cref="Socket"/> is being managed externally (for example once Server receives a Socket connection).
        /// </summary>
        public SocketClient(Socket socket) : base(socket) { }

        /// <summary>
        /// Initializes <see cref="Socket"/> to prepare it for first use.
        /// </summary>
        public SocketClient(SocketOptions socketOptions) : base(socketOptions) { }

        /// <summary>
        /// Begins a connect operation on <see cref="Socket"/>.
        /// </summary>
        public void ConnectAsync(string host, int port) {
            if (EventHandler == null)
                // EventHandler must be implemented to accuire connect event
                throw new ArgumentNullException(nameof(EventHandler));

            // get socket event for the operation
            var socketEvent = EventHandler.GetSocketEvent();

            // register the callback
            socketEvent.Completed += SocketEvent_Completed;

            // set the endpoint to connect to
            socketEvent.RemoteEndPoint = new DnsEndPoint(host, port);

            if (!Socket.ConnectAsync(socketEvent))
                // the operation completed synchronously probably due to some error, so invoke the callback immidietly to process the result
                SocketEvent_Completed(Socket, socketEvent);
        }

        /// <summary>
        /// Implements the connect method to process connected connetion.
        /// </summary>
        internal override void processClientConnect(SocketAsyncEventArgs e) {
            try {
                if (e.SocketError != SocketError.Success) {
                    // invoke the client handler to process the error in the implementation
                    ClientHandler?.ConnectionError(this, e);
                    return;
                }

                // invoke the client handler to process the event in the implementation
                ClientHandler?.ConnectionOpened(this);
            } finally {
                // signal the waiter
                ConnectTaskCompleteSignal.Set();
            }
        }

        /// <summary>
        /// Implements the receive method to process received data from <see cref="Socket"/>.
        /// Returns true to free up SocketAsyncEventArgs.
        /// </summary>
        internal override bool processClientReceive(SocketAsyncEventArgs e) {
            if (e.BytesTransferred == 0) {
                // BytesTransferred = 0 means connection was closed
                ClientHandler?.ConnectionClosed(this);
                return true;
            }

            switch (e.SocketError) {
                case SocketError.ConnectionReset:
                case SocketError.ConnectionAborted:
                case SocketError.OperationAborted:
                    // ConnectionReset is raised when the connection was closed - no reason to continue receive
                    ClientHandler?.ConnectionClosed(this);
                    return true;
            }

            if (e.SocketError != SocketError.Success) {
                // invoke the client handler to process the error in the implementation
                ClientHandler?.ConnectionError(this, e);
                return true;
            }

            // UserToken should contain the the implementation to manage the NetworkMessage
            var messageHandler = e.UserToken as NetworkMessageHandler;

            // process the received data in the message handler
            var isReceiveCompleted = messageHandler.CompleteReceive(e.BytesTransferred);
            if (isReceiveCompleted == null) {
                // the specified message size is over the limit
                e.SocketError = SocketError.MessageSize;

                // invoke the client handler to process the error in the implementation
                ClientHandler?.ConnectionError(this, e);
                return true;
            }

            if ((bool)isReceiveCompleted) {
                // finalize (deobfuscation etc), deserialize to complete message, and process in protocol
                ClientHandler?.ConnectionMessage(this, SerializationHandler.Deserialize(messageHandler.GetFinalized()));

                // message has been processed, so handler can be resetted
                messageHandler.Reset();
            }

            // resume receive
            receiveAsync(e);

            // the receive operation always continues on the same event object, so do not free it up
            return false;
        }

        /// <summary>
        /// Implements the send method to process sent data to <see cref="Socket"/>.
        /// Returns true to free up SocketAsyncEventArgs.
        /// </summary>
        internal override bool processClientSend(SocketAsyncEventArgs e) {
            if (e.BytesTransferred == 0)
                // BytesTransferred = 0 means connection was closed
                return true;

            if (e.SocketError != SocketError.Success) {
                // invoke the client handler to process the error in the implementation
                ClientHandler?.ConnectionError(this, e);
                return true;
            }

            // UserToken should contain the the implementation to manage the NetworkMessage
            var messageHandler = e.UserToken as NetworkMessageHandler;

            // process the sent data in the message handler
            if (!messageHandler.CompleteSend(e.BytesTransferred)) {
                // return false, to continue the send operation on the SocketAsyncEventArgs
                sendAsync(e);
                return false;
            }

            // the send operation completed, so can return true to release the event object
            return true;
        }

        /// <summary>
        /// Begins a receive operation on <see cref="Socket"/> using the <typeparamref name="T"/> message handler implementation
        /// </summary>
        public void ReceiveAsync<T>() where T : NetworkMessageHandler {
            if (EventHandler == null)
                // EventHandler must be implemented to accuire connect event
                throw new ArgumentNullException(nameof(EventHandler));

            if (SerializationHandler == null)
                // SerializationHandler must be implemented to transform the network message from binary data to T
                throw new ArgumentNullException(nameof(SerializationHandler));

            // get socket event for the operation
            var socketEvent = EventHandler.GetSocketEvent();

            // the UserToken holds a NetworkMessageHandler that manages the incoming data and the message implementation format
            socketEvent.UserToken = Activator.CreateInstance<T>();

            // do the receive operation
            receiveAsync(socketEvent);
        }

        /// <summary>
        /// Begins async receive on <see cref="Socket"/> 
        /// </summary>
        private void receiveAsync(SocketAsyncEventArgs socketEvent) {
            // UserToken should contain the the implementation to manage the NetworkMessage
            var messageHandler = socketEvent.UserToken as NetworkMessageHandler;

            // register the callback
            socketEvent.Completed += SocketEvent_Completed;

            // initialize the buffer to receive, allowing partial receive by instructions from messageHandler properties
            socketEvent.SetBuffer(
                messageHandler.Buffer,
                messageHandler.Offset,
                messageHandler.Length);

            if (!Socket.ReceiveAsync(socketEvent))
                // the operation completed synchronously probably due to some error, so invoke the callback immidietly to process the result
                SocketEvent_Completed(Socket, socketEvent);
        }

        /// <summary>
        /// Begins a send operation on <see cref="Socket"/> using the <typeparamref name="T"/> message handler implementation
        /// </summary>
        public void SendAsync<T>(T message) where T : NetworkMessageHandler {
            if (EventHandler == null)
                // EventHandler must be implemented to accuire connect event
                throw new ArgumentNullException(nameof(EventHandler));

            if (SerializationHandler == null)
                // SerializationHandler must be implemented to transform the network message into binary data
                throw new ArgumentNullException(nameof(SerializationHandler));

            // transform the network message into raw data and apply finalization (for encryption of data etc)
            message.SetFinalized(SerializationHandler.Serialize(message));

            // get socket event for the operation
            var socketEvent = EventHandler.GetSocketEvent();

            // UserToken should contain the the implementation to manage the NetworkMessage
            socketEvent.UserToken = message;

            sendAsync(socketEvent);
        }

        /// <summary>
        /// Begins async send on <see cref="Socket"/> 
        /// </summary>
        private void sendAsync(SocketAsyncEventArgs socketEvent) {
            // UserToken should contain the the implementation to manage the NetworkMessage
            var messageHandler = socketEvent.UserToken as NetworkMessageHandler;

            // register the callback
            socketEvent.Completed += SocketEvent_Completed;

            // initialize the buffer to send, allowing partial send by instructions from messageHandler properties
            socketEvent.SetBuffer(
                messageHandler.Buffer, 
                messageHandler.Offset, 
                messageHandler.Length);

            if (!Socket.SendAsync(socketEvent))
                // the operation completed synchronously probably due to some error, so invoke the callback immidietly to process the result
                SocketEvent_Completed(Socket, socketEvent);
        }

        /// <summary>
        /// Signal both connected ends if socket closure
        /// </summary>
        public void Shutdown() {
            Socket.Shutdown(SocketShutdown.Both);
        }

        internal override void disposeManagedObjects() {
            base.disposeManagedObjects();

            using (ConnectTaskCompleteSignal) ;
        }
    }
}
