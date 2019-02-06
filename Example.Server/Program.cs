using SocketNetwork.Default;
using System;

namespace SocketNetwork.Example.Server {
    internal class Program {
        /// <summary>
        /// Default pool based async socket event manager
        /// </summary>
        public static readonly SocketEventManager EventManager = new SocketEventManager();

        private static void Main(string[] args) {
            using (EventManager)
            using (var chatServer = new ChatServer()) {
                Console.WriteLine("Chat server starting..");
                chatServer.Start(44485, 100);

                Console.WriteLine("Chat server running.");
                Console.ReadKey(true);

                Console.WriteLine("Chat server stopping..");
                chatServer.Stop();

                Console.WriteLine("Chat server closed.");
            }

            Console.ReadKey(true);
        }
    }
}
