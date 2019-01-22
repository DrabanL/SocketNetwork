using SocketNetwork.Models;
using System.IO;
using System.Text;

namespace SocketNetwork.Example.Utilities.Models {
    public class ChatMessageSerializer : INetworkMessageSerializationHandler {
        public static readonly ChatMessageSerializer Instance = new ChatMessageSerializer();

        private ChatMessage onDeserialize(byte[] message) {
            using (var reader = new BinaryReader(new MemoryStream(message), Encoding.Unicode)) {
                var msg = new ChatMessage {
                    OpCode = reader.ReadByte(),
                    Sender = reader.ReadString()
                };

                if ((OpCodes)msg.OpCode == OpCodes.ConversationMessage)
                    msg.Text = reader.ReadString();

                return msg;
            }
        }

        private byte[] onSerialize(ChatMessage message) {
            using (var writer = new BinaryWriter(new MemoryStream(), Encoding.Unicode)) {
                writer.Write(message.OpCode);
                writer.Write(message.Sender);

                if ((OpCodes)message.OpCode == OpCodes.ConversationMessage)
                    writer.Write(message.Text);

                writer.Flush();
                return (writer.BaseStream as MemoryStream).ToArray();
            }
        }

        NetworkMessageHandler INetworkMessageSerializationHandler.Deserialize(byte[] message)
            => onDeserialize(message);

        byte[] INetworkMessageSerializationHandler.Serialize(NetworkMessageHandler message)
            => onSerialize(message as ChatMessage);
    }
}
