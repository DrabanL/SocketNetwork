namespace SocketNetwork.Example.Utilities.Models {
    public enum OpCodes : byte {
        ConversationJoin = 0xE0,
        ConversationMessage = 0xE1,
        ConversationLeave = 0xE2
    }
}
