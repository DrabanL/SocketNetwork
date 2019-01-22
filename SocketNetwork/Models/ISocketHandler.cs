using System.Net.Sockets;

namespace SocketNetwork.Models {
    public interface ISocketHandler {
        SocketAsyncEventArgs GetSocketEvent();
        void ReturnSocketEvent(SocketAsyncEventArgs e);
        void Release();
    }
}
