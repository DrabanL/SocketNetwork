using System.Net.Sockets;

namespace SocketNetwork.Models {
    /// <summary>
    /// Container for the socket options to use when constucting a Socket object.
    /// </summary>
    public struct SocketOptions {
        public AddressFamily AddressFamily;
        public SocketType SocketType;
        public ProtocolType ProtocolType;

        /// <summary>
        /// Returns IPv4 Tcp Stream socket options.
        /// </summary>
        public static SocketOptions Default =>
            new SocketOptions() {
                AddressFamily = AddressFamily.InterNetwork,
                ProtocolType = ProtocolType.Tcp,
                SocketType = SocketType.Stream,
            };
    }
}
