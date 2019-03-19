using RabanSoft.SocketNetwork.Default;
using System;
using System.Threading;

namespace RabanSoft.SocketNetwork.Example.Client {
    internal class Program {
        /// <summary>
        /// Default pool based async socket event manager
        /// </summary>
        public static readonly SocketEventManager EventManager = new SocketEventManager();

        private static string _nickname;
        private static ChatClient _chatClient;

        private static void Main(string[] args) {
            // join the conversation with a nickname
            Console.WriteLine("Nickname: ");
            Console.Title = _nickname = Console.ReadLine();

            using (EventManager)
            using (_chatClient = new ChatClient(_nickname)) {
                Console.WriteLine("connecting to server..");

                // we expect the chat server to run locally in port 44485
                _chatClient.ConnectAsync("localhost", 44485);

                // wait synchronously for the connection to be established
                _chatClient.ConnectTaskCompleteSignal.WaitOne();

                Thread chatInputThread = null;
                if (_chatClient.Socket.Connected) {

                    // get chat input on seperate thread, not to block our execution when doing Console.ReadLine
                    chatInputThread = new Thread(new ThreadStart(runChatInputLoop));
                    chatInputThread.Start();

                    // wait synchronously for the connection to be closed
                    SpinWait.SpinUntil(() => !_chatClient.Socket.Connected);
                }

                // 'enter' key will release the chat input loop
                Console.WriteLine("disconnected, press Enter to exit");

                if (chatInputThread != null)
                    // wait for chat input thread to exit
                    chatInputThread.Join();
                else
                    Console.ReadLine();
            }
        }

        private static void runChatInputLoop() {
            Console.WriteLine("Write msg: ");
            while (true) {
                string msg;
                // continue processing input until user writes 'exit'
                if ((msg = Console.ReadLine()).Equals("exit", StringComparison.InvariantCultureIgnoreCase)) {
                    // signal the server to end the connection on its end
                    _chatClient.Shutdown();
                    break;
                }

                if (!_chatClient.SendMessage(_nickname, msg))
                    // socket is not connected, so end input loop
                    break;
            }
        }
    }
}
