using System.Net.Sockets;

namespace SocketNetwork.Models {
    public interface IServerHandler {
        void ConnectionAcceptError(SocketAsyncEventArgs e);
        void ConnectionAccepted(Socket client);
    }
}
