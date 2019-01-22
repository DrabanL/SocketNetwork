using SocketNetwork.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SocketNetwork.Default {
    public class ProtocolMessage : NetworkMessageHandler {
        private List<byte> _messageData;
        private uint? _messageSize;

        public ProtocolMessage() {
            Buffer = new byte[512];
            _messageData = new List<byte>();

            Reset();
        }

        internal override void Reset() {
            Length = 4;
            Offset = 0;

            _messageData.Clear();
            _messageSize = null;
        }

        internal override bool CompleteSend(int len) {
            Offset += len;
            Length = Buffer.Length - Offset;
            return Offset == Buffer.Length;
        }

        internal override bool CompleteReceive(int len) {
            _messageData.AddRange(Buffer.Take(len));

            if (_messageSize == null) {
                var remRecv = 4 - _messageData.Count;
                if (remRecv > 0) {
                    Length = remRecv;
                    return false;
                }

                _messageSize = BitConverter.ToUInt32(
                    _messageData
                    .Take(4)
                    .ToArray(), 0);

                _messageData.Clear();
            }

            var remSize = _messageSize - _messageData.Count;
            Length = remSize > Buffer.Length ? Buffer.Length : (int)remSize;

            return remSize == 0;
        }

        public override byte[] GetFinalized() {
            return _messageData.ToArray();
        }

        public override void SetFinalized(byte[] messageData) {
            var packet = new List<byte>();
            packet.AddRange(BitConverter.GetBytes((uint)messageData.Length));
            packet.AddRange(messageData);

            Buffer = packet.ToArray();
            Length = Buffer.Length;
            Offset = 0;
        }
    }
}
