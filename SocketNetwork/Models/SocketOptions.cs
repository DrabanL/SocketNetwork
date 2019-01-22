using System.Net.Sockets;

namespace SocketNetwork.Models {
    public struct SocketOptions {
        public AddressFamily AddressFamily;
        public SocketType SocketType;
        public ProtocolType ProtocolType;
    }
}
