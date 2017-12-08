namespace ServerShared
{
    public enum MessageType : byte
    {
        ConnectMessage = 0,
        CreatePlayer = 5,
        RemovePlayer = 6,
        MoveData = 10,
    }
}
