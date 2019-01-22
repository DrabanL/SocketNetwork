namespace SocketNetwork.Models {
    public interface INetworkMessageSerializationHandler {
        NetworkMessageHandler Deserialize(byte[] message);
        byte[] Serialize(NetworkMessageHandler message);
    }
}
