using SocketNetwork.Default;
using System;
using System.Threading;

namespace SocketNetwork.Example.Client {
    internal class Program {
        /// <summary>
        /// Default pool based async socket event manager
        /// </summary>
        public static readonly SocketEventManager EventManager = new SocketEventManager();

        private static void Main(string[] args) {
            // join the conversation with a nickname
            Console.WriteLine("Nickname: ");
            var nickname = Console.ReadLine();
            Console.Title = nickname;

            using (EventManager)
            using (var chatClient = new ChatClient(nickname)) {
                Console.WriteLine("connecting to server..");

                // we expect the chat server to run locally in port 44485
                chatClient.ConnectAsync("localhost", 44485);

                // wait synchronously for the connection to be established
                SpinWait.SpinUntil(() => chatClient.Socket.Connected);

                Console.WriteLine("Write msg: ");
                while (true) {
                    // continue reading input until user writes 'exit'

                    var msg = Console.ReadLine();
                    if (msg.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                        break;

                    if (!chatClient.SendMessage(nickname, msg))
                        // socket is not connected, so end input loop
                        goto _DISCONNECTED;
                }

                // loop ended, so send leave operation
                chatClient.SendLeave(nickname);

                // give the server some time to accept and process the "leave" operation
                Console.WriteLine("disconnecting in 3 seconds..");
                Thread.Sleep(3000);

                // disconnect gracefully from server
                chatClient.DisconnectAsync();

                // wait synchronously for the connection to be closed
                SpinWait.SpinUntil(() => !chatClient.Socket.Connected);
            }

            _DISCONNECTED:
            Console.WriteLine("disconnected, press any key to close");
            Console.ReadKey(true);
        }
    }
}
