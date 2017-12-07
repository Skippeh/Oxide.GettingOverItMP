using System;
using System.Collections.Generic;
using System.Linq;
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
            
            if (ms >= nextSendTime)
            {
                nextSendTime = ms + server.UpdateTime;

                if (Players.Count <= 0)
                    return;

                Dictionary<int, PlayerMove> toSend = Players.Values.Where(plr => plr.Spawned).ToDictionary(plr => plr.Id, plr => plr.Movement);

                var writer = new NetDataWriter();
                writer.Put(MessageType.MoveData);
                writer.Put(toSend);

                Broadcast(writer, SendOptions.Sequenced);
            }
        }

        private void AddPeer(NetPeer peer)
        {
            var netPlayer = new NetPlayer(peer);
            Players[peer] = netPlayer;
        }

        private void RemovePeer(NetPeer peer)
        {
            if (!Players.ContainsKey(peer))
                return;

            int playerId = Players[peer].Id;

            Players.Remove(peer);
            
            var writer = new NetDataWriter();
            writer.Put(MessageType.RemovePlayer);
            writer.Put(playerId);
            Broadcast(writer, SendOptions.ReliableOrdered);
        }

        /// <summary>
        /// Sends a message to all spawned clients.
        /// </summary>
        private void Broadcast(NetDataWriter writer, SendOptions sendOptions)
        {
            foreach (var kv in Players.Where(kv => kv.Value.Spawned))
            {
                kv.Key.Send(writer, sendOptions);
            }
        }

        private void OnPeerConnected(NetPeer peer)
        {
            AddPeer(peer);
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            RemovePeer(peer);
        }

        private void OnReceiveData(NetPeer peer, NetDataReader reader)
        {
            try
            {
                NetPlayer player = Players[peer];
                MessageType messageType = (MessageType) reader.GetByte();

                switch (messageType)
                {
                    default: throw new UnexpectedMessageFromClientException(messageType);
                    case MessageType.MoveData:
                    {
                        player.Movement = reader.GetPlayerMove();
                        player.Spawned = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("OnReceiveData errored:\n" + ex);
                server.DisconnectPeer(peer);
            }
        }
    }
}
