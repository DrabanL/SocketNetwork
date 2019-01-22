using SocketNetwork.Default;

namespace SocketNetwork.Example.Utilities.Models {
    public class ChatMessage : EncryptedProtocolMessage {
        public byte OpCode;
        public string Sender;
        public string Text;
    }
}
