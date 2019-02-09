using SocketNetwork.Example.Utilities.Models;
using SocketNetwork.Models;
using System;
using System.Net.Sockets;

namespace SocketNetwork.Example.Client {
    /// <summary>
    /// Manages the chat client communication with server by extending <see cref="SocketClient"/> and implementing <see cref="IClientHandler"/>.
    /// </summary>
    internal class ChatClient : SocketClient, IClientHandler {
        /// <summary>
        /// The nickname that the user chose for himself.
        /// </summary>
        private readonly string _nickname;

        /// <summary>
        /// Initialize the chat client with the user's nickname.
        /// </summary>
        /// <param name="nickname"></param>
        public ChatClient(string nickname) : base() {
            _nickname = nickname;

            // implement the server communication in this class
            ClientHandler = this;

            // use the chat system serializer
            SerializationHandler = ChatMessageSerializer.Instance;

            // use the global socket event manager
            EventHandler = Program.EventManager;
        }

        /// <summary>
        /// Sends a 'Join' protocol command to chat server.
        /// </summary>
        private void sendJoin() {
            SendAsync(new ChatMessage() { // construct a new ChatMessage object that extends NetworkMessageHandler
                OpCode = (byte)OpCodes.ConversationJoin, // the 'Join' command code
                Sender = _nickname // the nickname that the user has chosen
            });
        }

        /// <summary>
        /// Sends a 'Message' protocol command to chat server. Returns false if socket is not connected. otherwise true.
        /// </summary>
        internal bool SendMessage(string sender, string msg) {
            if (!Socket.Connected)
                // the socket may have been closed previously but it was not managed in input context
                return false;

            SendAsync(new ChatMessage() { // construct a new ChatMessage object that extends NetworkMessageHandler
                OpCode = (byte)OpCodes.ConversationMessage,
                Sender = sender, // the nickname that the user has chosen
                Text = msg // the message text
            });

            return true;
        }

        /// <summary>
        /// Sends a 'Leave' protocol command to chat server for notification and graceful closure of the connection. Returns false if socket is not connected. otherwise true.
        /// </summary>
        internal void SendLeave(string nickname) {
            if (!Socket.Connected)
                // the socket may have been closed previously but it was not managed in input context
                return;

            SendAsync(new ChatMessage() { // construct a new ChatMessage object that extends NetworkMessageHandler
                OpCode = (byte)OpCodes.ConversationLeave,
                Sender = nickname // the nickname that the user has chosen
            });
        }

        /// <summary>
        /// Processes the closed chat client connection.
        /// </summary>
        private void onConnectionClosed(ChatClient client) {
            Console.WriteLine("server connection closed");
        }

        /// <summary>
        /// Processes the opened chat client connection.
        /// </summary>
        private void onConnectionOpened(ChatClient client) {
            // now that we are connected to server, can send the 'Join' command
            sendJoin();

            // begin receving data from server, in a ChatMessage object that extends NetworkMessageHandler
            client.ReceiveAsync<ChatMessage>();
        }

        /// <summary>
        /// Processes the error that occured in the async operation.
        /// </summary>
        private void onConnectionError(ChatClient client, SocketAsyncEventArgs socketEvent) {
            if (disposedValue)
                // the internal Socket is being closed when disposing, so error on ongoing 'receive' operation may raise, so it should be ignored
                return;

            // error in the socket event likely breaks the entire socket, so make sure to cleanup
            using (this) {
                Console.WriteLine($"operation error! {socketEvent.LastOperation} {socketEvent.SocketError}");
            }
        }

        /// <summary>
        /// Processes chat system protocol received from server.
        /// </summary>
        private void onConnectionMessage(ChatClient client, ChatMessage message) {
            switch ((OpCodes)message.OpCode) {
                case OpCodes.ConversationMessage:
                    // write the chat message received from server
                    Console.WriteLine($"[{message.Sender}]: {message.Text}");
                    break;
            }
        }

        /// <summary>
        /// Handles closed socket client connection.
        /// </summary>
        void IClientHandler.ConnectionClosed(SocketClient client)
            // SocketClient is actually ChatClient so it can be casted to our own type
            => onConnectionClosed(client as ChatClient);

        /// <summary>
        /// Handles opened socket client connection.
        /// </summary>
        void IClientHandler.ConnectionOpened(SocketClient client)
            // SocketClient is actually ChatClient so it can be casted to our own type
            => onConnectionOpened(client as ChatClient);

        /// <summary>
        /// Handles socket client async operation error.
        /// </summary>
        void IClientHandler.ConnectionError(SocketClient client, SocketAsyncEventArgs socketEvent)
            // SocketClient is actually ChatClient so it can be casted to our own type
            => onConnectionError(client as ChatClient, socketEvent);

        /// <summary>
        /// Handles socket client message.
        /// </summary>
        void IClientHandler.ConnectionMessage(SocketClient client, NetworkMessageHandler message)
            // SocketClient is actually ChatClient so it can be casted to our own type
            => onConnectionMessage(client as ChatClient, message as ChatMessage);
    }
}
