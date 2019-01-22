namespace SocketNetwork.Default {
    public class EncryptedProtocolMessage : ProtocolMessage {
        public override byte[] GetFinalized() {
            var data = base.GetFinalized();
            MessageEncryption.Xor(data);

            return data;
        }

        public override void SetFinalized(byte[] messageData) {
            MessageEncryption.Xor(messageData);
            base.SetFinalized(messageData);
        }
    }
}
