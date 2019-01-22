namespace SocketNetwork.Default {
    internal static class MessageEncryption {
        private static byte[] _key = new byte[] { 0xFA, 0x01, 0xC5 };

        public static void Xor(byte[] data) {
            for (int i = 0; i < data.Length; ++i)
                data[i] ^= _key[i % _key.Length];
        }
    }
}
