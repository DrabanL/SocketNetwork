using SocketNetwork.Example.Utilities.Models;
using SocketNetwork.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace SocketNetwork.Example.Server {
    /// <summary>
    /// Manages chat server by extending <see cref="SocketServer"/> and handles its connection/protocol with clients by implementing <see cref="IServerHandler"/> and <seealso cref="IClientHandler"/>.
    /// </summary>
    internal class ChatServer : SocketServer, IServerHandler, IClientHandler {
        /// <summary>
        /// Stores all managed connections (participants in the chat conversation).
        /// </summary>
        private readonly List<ChatMember> _clients = new List<ChatMember>();

        /// <summary>
        /// Initializes the server object and registers handlers.
        /// </summary>
        public ChatServer() : base() {
            // register the server handler (process new connections) to be managed in local class
            ServerHandler = this;

            // use the global pool for socket events
            EventHandler = Program.EventManager;
        }

        /// <summary>
        /// Handle error on async accept operation.
        /// </summary>
        void IServerHandler.ConnectionAcceptError(SocketAsyncEventArgs e) {
            Console.WriteLine($"Server accept error! {e.SocketError}");
        }

        /// <summary>
        /// Handle received connection on the socket object.
        /// </summary>
        void IServerHandler.ConnectionAccepted(Socket socket) {
            // create new chat member from received socket
            var client = new ChatMember(socket);

            // add the new connection to managed list
            _clients.Add(client);

            // register the client handler (process chat messages) to be managed in local class
            client.ClientHandler = this;

            // use the chat system serializer
            client.SerializationHandler = ChatMessageSerializer.Instance;

            // use the global pool for socket events
            client.EventHandler = Program.EventManager;

            // begin receving data from chat member, in a ChatMessage object that extends NetworkMessageHandler
            client.ReceiveAsync<ChatMessage>();

            Console.WriteLine($"(Server) client connected {client.Socket.RemoteEndPoint}");
        }

        /// <summary>
        /// Sends a chat message to a specific chat member.
        /// </summary>
        private void sendAllMessage(string sender, string message, params string[] exclusions) {
            _clients
                .Where(v => v.Name != null && !exclusions.Contains(v.Name)) // all connected clients excluding specified members by name
                .ToList() // to be able to "ForEach" in LINQ style
                .ForEach(client => client.SendAsync(new ChatMessage() { // construct a new ChatMessage object that extends NetworkMessageHandler
                    OpCode = (byte)OpCodes.ConversationMessage,
                    Sender = sender,
                    Text = message
                }));
        }

        /// <summary>
        /// Sends a chat message to a specific chat member.
        /// </summary>
        private void sendMessage(string to, string sender, string message) {
            _clients
                .Where(v => v.Name?.Equals(to) ?? false) // find by name (fails if null)
                .First() // should only be one result
                .SendAsync(new ChatMessage() { // construct a new ChatMessage object that extends NetworkMessageHandler
                    OpCode = (byte)OpCodes.ConversationMessage,
                    Sender = sender,
                    Text = message
                });
        }

        /// <summary>
        /// Processes the closed chat member connection.
        /// </summary>
        private void onConnectionClosed(ChatMember client) {
            // make sure the connection is properly disposed
            using (client) {
                // notify all participants of the leave (excluding the member that left)
                sendAllMessage("Officer", $"{client.Name} has left the conversation.", client.Name);

                // remove the member from managed connections
                _clients.Remove(client);

                Console.WriteLine($"(Server) client disconnected {client.Socket.RemoteEndPoint}");
            }
        }

        /// <summary>
        /// Processes the error that occured in the async operation.
        /// </summary>
        private void onConnectionError(ChatMember client, SocketAsyncEventArgs socketEvent) {
            Console.WriteLine($"(Server) client {socketEvent.LastOperation} socket error: {socketEvent.SocketError}");

            // check if we already registered the connection as a chat member
            if (!_clients.Contains(client)) {
                // unhandled error possibly destroyed the connection, so close it and cleanup
                using (client) ;
                return;
            }
            
            // unhandled error possibly destroyed the connection, so close it and cleanup
            onConnectionClosed(client);
        }

        /// <summary>
        /// Processes chat system protocol received from chat member.
        /// </summary>
        private void onConnectionMessage(ChatMember client, ChatMessage message) {
            switch ((OpCodes)message.OpCode) {
                case OpCodes.ConversationJoin:
                    // identify the chat member with a nickname
                    client.Name = message.Sender;

                    // notify all chat participants of the new joined member (excluding the joined member)
                    sendAllMessage("Officer", $"{message.Sender} has joined the conversation.", message.Sender);

                    // send a welcome message to the new participant
                    sendMessage(message.Sender, $"Officer", $"Welcome {message.Sender}!");
                    break;
                case OpCodes.ConversationMessage:
                    // forward the chat message to all participants in the conversation
                    sendAllMessage(message.Sender, message.Text);
                    break;
                case OpCodes.ConversationLeave:
                    // gracefully close the connection with the participant
                    onConnectionClosed(client);
                    break;
            }
        }

        /// <summary>
        /// Handles closed socket client connection.
        /// </summary>
        void IClientHandler.ConnectionClosed(SocketClient client)
            // SocketClient is actually ChatMember so it can be casted to our own type
            => onConnectionClosed(client as ChatMember);

        /// <summary>
        /// Handles opened socket client connection.
        /// </summary>
        void IClientHandler.ConnectionOpened(SocketClient client)
            // this method is normally not being invoked on SocketClient implementation when in Server mode
            => new NotImplementedException();

        /// <summary>
        /// Handles socket client async operation error.
        /// </summary>
        void IClientHandler.ConnectionError(SocketClient client, SocketAsyncEventArgs socketEvent)
            // SocketClient is actually ChatMember so it can be casted to our own type
            => onConnectionError(client as ChatMember, socketEvent);

        /// <summary>
        /// Handles socket client message.
        /// </summary>
        void IClientHandler.ConnectionMessage(SocketClient client, NetworkMessageHandler message)
            // SocketClient is actually ChatMember so it can be casted to our own type
            => onConnectionMessage(client as ChatMember, message as ChatMessage);

        /// <summary>
        /// Overrides dispose of base class to handle disposing of local objects.
        /// </summary>
        protected override void Dispose(bool disposing) {
            // make sure all connections are closed and disposed
            foreach (var client in _clients)
                // socket closure is handled in ChatMemeber object dispose
                using (client) ;

            _clients.Clear();

            // continue dispose on base object
            base.Dispose(disposing);
        }
    }
}
