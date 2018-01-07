namespace ServerShared
{
    public enum MessageType : byte
    {
        HandshakeResponse = 1,
        CreatePlayer = 5,
        RemovePlayer = 6,
        SpectateTarget = 7,
        ClientStopSpectating = 8,
        MoveData = 10,
        ChatMessage = 20,
        PlayerPotProperties = 21,
        PlayerWins = 22,
    }
}
