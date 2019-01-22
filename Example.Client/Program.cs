using SocketNetwork.Default;
using System;
using System.Threading;

namespace SocketNetwork.Example.Client {
    internal class Program {
        public static readonly BufferManager BufferManager = new BufferManager();

        private static void Main(string[] args) {
            Console.WriteLine("Nickname: ");
            var nickname = Console.ReadLine();
            Console.Title = nickname;

            using (BufferManager)
            using (var chatClient = new ChatClient(nickname)) {
                Console.WriteLine("connecting to server..");

                chatClient.ConnectAsync("localhost", 44485);
                SpinWait.SpinUntil(() => chatClient.Socket.Connected);

                Console.WriteLine("Write msg: ");
                while (true) {
                    var msg = Console.ReadLine();
                    if (msg.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                        break;

                    if (!chatClient.SendMessage(nickname, msg))
                        break;
                }

                chatClient.SendLeave(nickname);
            }

            Console.WriteLine("disconnected, press any key to close");
            Console.ReadKey(true);
        }
    }
}
