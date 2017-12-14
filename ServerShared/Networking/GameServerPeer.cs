using System;
using System.Net;
using Lidgren.Network;

namespace ServerShared
{
    public class GameServerPeer : NetServer, IGamePeer
    {
        public event ConnectedEventHandler Connected;
        public event DisconnectedEventHandler Disconnected;
        public event DataReceivedEventHandler DataReceived;

        /// <summary>Invoked when the server gets a DiscoveryRequest message.</summary>
        public event DiscoveryRequestEventHandler DiscoveryRequest;

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

        public void InvokeDiscoveryRequest(object sender, NetIncomingMessage message)
        {
            DiscoveryRequest?.Invoke(sender, message);
        }

        public void Update()
        {
            StaticGamePeer.Update(this);
        }
    }
}
