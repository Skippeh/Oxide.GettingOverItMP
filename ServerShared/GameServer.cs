using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using ServerShared.Player;

namespace ServerShared
{
    public class GameServer
    {
        public readonly int Port;
        public readonly bool ListenServer;

        public readonly Dictionary<NetPeer, NetPlayer> Players = new Dictionary<NetPeer, NetPlayer>();

        private readonly EventBasedNetListener listener;
        private readonly NetManager server;

        private double nextSendTime = 0;

        public GameServer(int maxConnections, int port, bool listenServer)
        {
            if (maxConnections <= 0)
                throw new ArgumentException("Max connections needs to be > 0.");

            listener = new EventBasedNetListener();

            listener.PeerConnectedEvent += OnPeerConnected;
            listener.PeerDisconnectedEvent += OnPeerDisconnected;
            listener.NetworkReceiveEvent += OnReceiveData;

            server = new NetManager(listener, maxConnections, "GOIMP");
            Port = port;
            ListenServer = listenServer;

            server.UpdateTime = 33; // Send/receive 30 times per second.
            
            if (listenServer)
            {
                server.NatPunchEnabled = true;
            }
        }

        public void Start()
        {
            server.Start(Port);
        }

        public void Stop()
        {
            server.Stop();
        }

        public void Update()
        {
            server.PollEvents();

            double ms = DateTime.Now.Ticks / 10_000d;

            if (ms > nextSendTime)
            {
                nextSendTime = ms + server.UpdateTime;

                Console.WriteLine("Send update");
            }
        }

        private void OnPeerConnected(NetPeer peer)
        {
            AddPeer(peer);
        }

        private void AddPeer(NetPeer peer)
        {
            var netPlayer = new NetPlayer(peer);
            Players[peer] = netPlayer;

            // Todo: send message informing other clients the player has joined.
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            RemovePeer(peer);
        }

        private void RemovePeer(NetPeer peer)
        {
            if (Players.ContainsKey(peer))
                RemovePeer(peer);

            // Todo: send message informing other clients the player has left.
        }

        private void OnReceiveData(NetPeer peer, NetDataReader reader)
        {
            try
            {
                MessageType messageType = (MessageType) reader.GetByte();
            }
            catch (Exception ex)
            {
                Console.WriteLine("OnReceiveData errored:\n" + ex);
                server.DisconnectPeer(peer);
            }
        }
    }
}
