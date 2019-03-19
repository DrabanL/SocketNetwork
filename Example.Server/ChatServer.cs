using RabanSoft.SocketNetwork.Example.Utilities;
using RabanSoft.SocketNetwork.Example.Utilities.Models;
using RabanSoft.SocketNetwork.Models;
using System;
using System.Linq;
using System.Net.Sockets;

namespace RabanSoft.SocketNetwork.Example.Server {
    /// <summary>
    /// Manages chat server by extending <see cref="SocketServer"/> and handles its connection/protocol with clients by implementing <see cref="IServerHandler"/> and <seealso cref="IClientHandler"/>.
    /// </summary>
    internal class ChatServer : SocketServer, IServerHandler, IClientHandler {
        /// <summary>
        /// Stores all managed connections (participants in the chat conversation).
        /// </summary>
        private readonly ThreadSafeList<ChatMember> _clients = new ThreadSafeList<ChatMember>();

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
            Console.WriteLine($"accept error! {e.SocketError}");
        }

        /// <summary>
        /// Handle received connection on the socket object.
        /// </summary>
        void IServerHandler.ConnectionAccepted(Socket socket) {
            Console.WriteLine($"client {socket.RemoteEndPoint} connected");

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
            Console.WriteLine($"client {client.Socket.RemoteEndPoint} disconnected");

            // make sure the connection is properly disposed
            using (client)
                // remove the member from managed connections
                _clients.Remove(client);

            // notify all participants of the leave (excluding the member that left)
            sendAllMessage("Officer", $"{client.Name} has left the conversation.", client.Name);
        }

        /// <summary>
        /// Processes the error that occured in the async operation.
        /// </summary>
        private void onConnectionError(ChatMember client, SocketAsyncEventArgs socketEvent) {
            Console.WriteLine($"client {client?.Socket?.RemoteEndPoint} socket operation {socketEvent.LastOperation} error: {socketEvent.SocketError}");

            if (socketEvent.SocketError == SocketError.MessageSize) {
                Console.WriteLine("Message size limit reached.");

                client.Shutdown();
            }
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

        public override void Stop() {
            base.Stop();

            // make sure all connections are notified of server close
            foreach (var client in _clients)
                // signal the cleint to end the connection on its end
                client.Shutdown();
        }
    }
}
