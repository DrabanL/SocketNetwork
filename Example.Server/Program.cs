using RabanSoft.SocketNetwork.Default;
using System;

namespace RabanSoft.SocketNetwork.Example.Server {
    internal class Program {
        /// <summary>
        /// Default pool based async socket event manager
        /// </summary>
        public static readonly SocketEventManager EventManager = new SocketEventManager();

        private static void Main(string[] args) {
            // the async socket event pool object and server must be cleaned up after use
            using (EventManager)
            using (var chatServer = new ChatServer()) {
                Console.WriteLine("Chat server starting..");

                // start listening for connections via tcp port 44485 with up to 100 pending connections
                chatServer.Start(44485, 100);

                Console.WriteLine("Chat server running.");
                Console.WriteLine("Press any key to abort.");

                // block thread to allow chat server to continue running
                Console.ReadKey(true);

                Console.WriteLine("Chat server stopping..");

                // close the listener
                chatServer.Stop();

                Console.WriteLine("Chat server closed.");
            }

            Console.WriteLine("Press any key to exit..");
            Console.ReadKey(true);
        }
    }
}
