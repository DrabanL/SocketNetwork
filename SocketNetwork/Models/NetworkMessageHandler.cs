namespace SocketNetwork.Models {
    public abstract class NetworkMessageHandler {
        internal byte[] Buffer { get; set; }
        internal int Offset { get; set; }
        internal int Length { get; set; }

        internal abstract bool CompleteSend(int len);
        internal abstract bool CompleteReceive(int len);
        internal abstract void Reset();

        public abstract byte[] GetFinalized();
        public abstract void SetFinalized(byte[] data);
    }
}
