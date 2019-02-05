using System.Net.Sockets;

namespace SocketNetwork.Models {
    /// <summary>
    /// Handles socket server incoming connections.
    /// </summary>
    public interface IServerHandler {
        /// <summary>
        /// Handles error on client accept operation.
        /// </summary>
        void ConnectionAcceptError(SocketAsyncEventArgs e);

        /// <summary>
        /// Handles accepted connection.
        /// </summary>
        void ConnectionAccepted(Socket client);
    }
}
