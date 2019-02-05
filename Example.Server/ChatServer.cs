using SocketNetwork.Example.Utilities.Models;
using SocketNetwork.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace SocketNetwork.Example.Server {
    internal class ChatServer : SocketServer, IServerHandler, IClientHandler {
        private readonly List<ChatMember> _clients = new List<ChatMember>();

        public ChatServer() : base() {
            ServerHandler = this;
            EventHandler = Program.EventManager;
        }

        void IServerHandler.ConnectionAcceptError(SocketAsyncEventArgs e) {
            Console.WriteLine($"Server accept error! {e.SocketError}");
        }

        void IServerHandler.ConnectionAccepted(Socket socket) {
            var client = new ChatMember(socket);

            _clients.Add(client);

            client.ClientHandler = this;
            client.SerializationHandler = ChatMessageSerializer.Instance;
            client.EventHandler = Program.EventManager;
            client.ReceiveAsync<ChatMessage>();

            Console.WriteLine($"(Server) client connected {client.Socket.RemoteEndPoint}");
        }

        private void sendAllMessage(string sender, string message, params string[] exclusions) {
            _clients
                .Where(v => v.Name != null && !exclusions.Contains(v.Name))
                .ToList()
                .ForEach(client => client.SendAsync(new ChatMessage() {
                    OpCode = (byte)OpCodes.ConversationMessage,
                    Sender = sender,
                    Text = message
                }));
        }

        private void sendMessage(string to, string sender, string message) {
            _clients
                .Where(v => v.Name?.Equals(to) ?? false)
                .First()
                .SendAsync(new ChatMessage() {
                    OpCode = (byte)OpCodes.ConversationMessage,
                    Sender = sender,
                    Text = message
                });
        }

        private void onConnectionClosed(ChatMember client) {
            using (client) {
                sendAllMessage("Officer", $"{client.Name} has left the conversation.", client.Name);

                _clients.Remove(client);

                Console.WriteLine($"(Server) client disconnected {client.Socket.RemoteEndPoint}");
            }
        }

        private void onConnectionError(ChatMember client, SocketAsyncEventArgs socketEvent) {
            Console.WriteLine($"(Server) client {socketEvent.LastOperation} socket error: {socketEvent.SocketError}");

            if (!_clients.Contains(client))
                using (client) ;
            else
                onConnectionClosed(client);
        }

        private void onConnectionMessage(ChatMember client, ChatMessage message) {
            switch ((OpCodes)message.OpCode) {
                case OpCodes.ConversationJoin:
                    client.Name = message.Sender;

                    sendAllMessage("Officer", $"{message.Sender} has joined the conversation.", message.Sender);
                    sendMessage(message.Sender, $"Officer", $"Welcome {message.Sender}!");
                    break;
                case OpCodes.ConversationMessage:
                    sendAllMessage(message.Sender, message.Text);
                    break;
                case OpCodes.ConversationLeave:
                    //sendAllMessage("Officer", $"{message.Sender} has left the conversation.", message.Sender);
                    break;
            }
        }

        void IClientHandler.ConnectionClosed(SocketClient client)
            => onConnectionClosed(client as ChatMember);

        void IClientHandler.ConnectionOpened(SocketClient client)
            => new NotImplementedException();

        void IClientHandler.ConnectionError(SocketClient client, SocketAsyncEventArgs socketEvent)
            => onConnectionError(client as ChatMember, socketEvent);

        void IClientHandler.ConnectionMessage(SocketClient client, NetworkMessageHandler message)
            => onConnectionMessage(client as ChatMember, message as ChatMessage);

        protected override void Dispose(bool disposing) {
            foreach (var client in _clients)
                using (client) ;

            _clients.Clear();

            base.Dispose(disposing);
        }
    }
}
