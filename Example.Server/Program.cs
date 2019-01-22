using SocketNetwork.Default;
using System;

namespace SocketNetwork.Example.Server {
    internal class Program {
        public static readonly BufferManager BufferManager = new BufferManager();

        private static void Main(string[] args) {
            using (BufferManager)
            using (var chatServer = new ChatServer()) {
                chatServer.Start(44485, 100);

                Console.WriteLine("Chat server running.");
                Console.ReadKey(true);
            }
        }
    }
}
