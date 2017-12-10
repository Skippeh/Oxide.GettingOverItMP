namespace ServerShared
{
    public enum MessageType : byte
    {
        ClientHandshake = 0,
        HandshakeResponse,
        CreatePlayer = 5,
        RemovePlayer = 6,
        MoveData = 10,
        ChatMessage = 20
    }
}
