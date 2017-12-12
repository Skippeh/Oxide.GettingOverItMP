using Lidgren.Network;

namespace ServerShared
{
    public class GameServerPeer : NetServer, IGamePeer
    {
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        public event DataReceivedEventHandler DataReceived;

        public GameServerPeer(NetPeerConfiguration config) : base(config)
        {
        }

        public void InvokeConnected(object sender, ConnectedEventArgs args)
        {
            Connected?.Invoke(sender, args);
        }

        public void InvokeDisconnected(object sender, DisconnectedEventArgs args)
        {
            Disconnected?.Invoke(sender, args);
        }

        public void InvokeDataReceived(object sender, DataReceivedEventArgs args)
        {
            DataReceived?.Invoke(sender, args);
        }

        public void Update()
        {
            StaticGamePeer.Update(this);
        }
    }
}
