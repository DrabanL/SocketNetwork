using System.Net.Sockets;

namespace SocketNetwork.Example.Server {
    internal class ChatMember : SocketClient {
        public string Name;

        public ChatMember(Socket socket) : base(socket) { }
    }
}
