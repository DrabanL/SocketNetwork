using System.Net.Sockets;

namespace SocketNetwork.Models {
    public interface IClientHandler {
        void ConnectionClosed(SocketClient client);
        void ConnectionOpened(SocketClient client);
        void ConnectionError(SocketClient client, SocketAsyncEventArgs socketEvent);
        void ConnectionMessage(SocketClient client, NetworkMessageHandler message);
    }
}
