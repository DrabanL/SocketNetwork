using System.Net.Sockets;

namespace RabanSoft.SocketNetwork.Models {
    /// <summary>
    /// Handles socket client event operations.
    /// </summary>
    public interface IClientHandler {
        /// <summary>
        /// Handles closed socket client connection.
        /// </summary>
        void ConnectionClosed(SocketClient client);

        /// <summary>
        /// Handles opened socket client connection.
        /// </summary>
        void ConnectionOpened(SocketClient client);

        /// <summary>
        /// Handles socket client async operation error.
        /// </summary>
        void ConnectionError(SocketClient client, SocketAsyncEventArgs socketEvent);

        /// <summary>
        /// Handles socket client message.
        /// </summary>
        void ConnectionMessage(SocketClient client, NetworkMessageHandler message);
    }
}
