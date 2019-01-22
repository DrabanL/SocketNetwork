using SocketNetwork.Example.Utilities.Models;
using SocketNetwork.Models;
using System;
using System.Net.Sockets;

namespace SocketNetwork.Example.Client {
    internal class ChatClient : SocketClient, IClientHandler {
        private readonly string _nickname;

        public ChatClient(string nickname) : base() {
            _nickname = nickname;

            ClientHandler = this;
            SerializationHandler = ChatMessageSerializer.Instance;
            SocketHandler = Program.BufferManager;
        }

        internal void SendJoin() {
            SendAsync(new ChatMessage() {
                OpCode = (byte)OpCodes.ConversationJoin,
                Sender = _nickname
            });
        }

        internal bool SendMessage(string sender, string msg) {
            if (!Socket.Connected)
                return false;

            SendAsync(new ChatMessage() {
                OpCode = (byte)OpCodes.ConversationMessage,
                Sender = sender,
                Text = msg
            });

            return true;
        }

        internal void SendLeave(string nickname) {
            if (!Socket.Connected)
                return;

            SendAsync(new ChatMessage() {
                OpCode = (byte)OpCodes.ConversationLeave,
                Sender = nickname
            });
        }

        private void onConnectionClosed(ChatClient client) {
            Console.WriteLine("server connection closed");
        }

        private void onConnectionOpened(ChatClient client) {
            SendJoin();

            client.ReceiveAsync<ChatMessage>();
        }

        private void onConnectionError(ChatClient client, SocketAsyncEventArgs socketEvent) {
            if (disposedValue)
                return;

            using (this) {
                Console.WriteLine($"operation error! {socketEvent.LastOperation} {socketEvent.SocketError}");
            }
        }

        private void onConnectionMessage(ChatClient client, ChatMessage message) {
            switch ((OpCodes)message.OpCode) {
                case OpCodes.ConversationMessage:
                    Console.WriteLine($"[{message.Sender}]: {message.Text}");
                    break;
            }
        }

        void IClientHandler.ConnectionClosed(SocketClient client)
            => onConnectionClosed(client as ChatClient);

        void IClientHandler.ConnectionOpened(SocketClient client)
            => onConnectionOpened(client as ChatClient);

        void IClientHandler.ConnectionError(SocketClient client, SocketAsyncEventArgs socketEvent)
            => onConnectionError(client as ChatClient, socketEvent);

        void IClientHandler.ConnectionMessage(SocketClient client, NetworkMessageHandler message)
            => onConnectionMessage(client as ChatClient, message as ChatMessage);
    }
}
