namespace ServerShared
{
    public interface IGamePeer
    {
        event ConnectedEventHandler Connected;
        event DisconnectedEventHandler Disconnected;
        event DataReceivedEventHandler DataReceived;

        void InvokeConnected(object sender, ConnectedEventArgs args);
        void InvokeDisconnected(object sender, DisconnectedEventArgs args);
        void InvokeDataReceived(object sender, DataReceivedEventArgs args);
    }

    public delegate void ConnectedEventHandler(object sender, ConnectedEventArgs args);
    public delegate void DisconnectedEventHandler(object sender, DisconnectedEventArgs args);
    public delegate void DataReceivedEventHandler(object sender, DataReceivedEventArgs args);
}
