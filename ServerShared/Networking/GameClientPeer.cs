using System;
using Lidgren.Network;

namespace ServerShared
{
    public class GameClientPeer : NetClient, IGamePeer
    {
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        public event DataReceivedEventHandler DataReceived;
        
        public GameClientPeer(NetPeerConfiguration config) : base(config)
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

        public void InvokeDiscoveryRequest(object sender, NetIncomingMessage message)
        {
            // Clients will never receive this.
        }

        public void Update()
        {
            StaticGamePeer.Update(this);
        }
    }
}
