using System;
using System.Collections.Generic;
using System.Linq;
using LiteNetLib;
using LiteNetLib.Utils;
using ServerShared.Player;
using UnityEngine;

namespace ServerShared
{
    public class GameServer
    {
        class PendingConnection
        {
            public readonly NetPeer Peer;
            public readonly DateTime JoinTime;

            public PendingConnection(NetPeer peer, DateTime joinTime)
            {
                Peer = peer;
                JoinTime = joinTime;
            }
        }

        public static readonly TimeSpan PendingConnectionTimeout = new TimeSpan(0, 0, 0, 5); // 5 seconds

        public readonly int Port;
        public readonly bool ListenServer;

        public readonly Dictionary<NetPeer, NetPlayer> Players = new Dictionary<NetPeer, NetPlayer>();

        private readonly EventBasedNetListener listener;
        private readonly NetManager server;

        private double nextSendTime = 0;
        private readonly List<PendingConnection> pendingConnections = new List<PendingConnection>();

        public GameServer(int maxConnections, int port, bool listenServer)
        {
            if (maxConnections <= 0)
                throw new ArgumentException("Max connections needs to be > 0.");

            listener = new EventBasedNetListener();

            listener.PeerConnectedEvent += OnPeerConnected;
            listener.PeerDisconnectedEvent += OnPeerDisconnected;
            listener.NetworkReceiveEvent += OnReceiveData;

            server = new NetManager(listener, maxConnections, SharedConstants.AppName);
            Port = port;
            ListenServer = listenServer;

            server.UpdateTime = 33; // Send/receive 30 times per second.
            
            if (listenServer)
            {
                // Todo: Implement NAT punchthrough.
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

            // Disconnect timed out pending connections
            foreach (var connection in pendingConnections.ToList())
            {
                if (DateTime.UtcNow - connection.JoinTime >= PendingConnectionTimeout)
                {
                    Console.WriteLine("Disconnecting pending connection (handshake timeout)");
                    DisconnectPeer(connection.Peer, DisconnectReason.HandshakeTimeout);
                }
            }

            double ms = DateTime.UtcNow.Ticks / 10_000d;
            
            if (ms >= nextSendTime)
            {
                nextSendTime = ms + server.UpdateTime;

                if (Players.Count <= 0)
                    return;

                Dictionary<int, PlayerMove> toSend = Players.Values.ToDictionary(plr => plr.Id, plr => plr.Movement);

                var writer = new NetDataWriter();
                writer.Put(MessageType.MoveData);
                writer.Put(toSend);

                Broadcast(writer, SendOptions.Sequenced);
            }
        }

        private NetPlayer AddPeer(NetPeer peer, string playerName)
        {
            var netPlayer = new NetPlayer(peer, playerName);
            Players[peer] = netPlayer;

            var writer = new NetDataWriter();
            writer.Put(MessageType.HandshakeResponse);
            writer.Put(netPlayer.Id);
            writer.Put(netPlayer.Name);

            var allPlayers = Players.Values.Where(plr => plr.Peer != peer).ToList();
            var allNames = allPlayers.ToDictionary(plr => plr.Id, plr => plr.Name);
            var allPlayersDict = allPlayers.ToDictionary(plr => plr.Id, plr => plr.Movement);
            writer.Put(allNames);
            writer.Put(allPlayersDict);
            peer.Send(writer, SendOptions.ReliableOrdered);

            Console.WriteLine($"Added peer from {peer.EndPoint} with id {netPlayer.Id} (total: {Players.Count})");
            return netPlayer;
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

            Console.WriteLine($"Removed peer from {peer.EndPoint} with id {playerId} (total: {Players.Count})");
        }

        /// <summary>
        /// Sends a message to all spawned clients.
        /// </summary>
        private void Broadcast(NetDataWriter writer, SendOptions sendOptions, NetPeer except = null)
        {
            foreach (var kv in Players)
            {
                if (except == null || kv.Key != except)
                {
                    kv.Key.Send(writer, sendOptions);
                }
            }
        }

        private void DisconnectPeer(NetPeer peer, DisconnectReason reason)
        {
            Console.WriteLine($"Disconnecting peer from {peer.EndPoint}: {reason}");

            var writer = new NetDataWriter();
            writer.Put(reason);
            server.DisconnectPeer(peer, writer);
        }

        private void OnPeerConnected(NetPeer peer)
        {
            // Todo: protect against mass connections from the same ip.

            Console.WriteLine($"Connection from {peer.EndPoint}");
            pendingConnections.Add(new PendingConnection(peer, DateTime.UtcNow));
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectinfo)
        {
            Console.WriteLine($"Connection gone from {peer.EndPoint} ({disconnectinfo.Reason})");
            pendingConnections.RemoveAll(conn => conn.Peer == peer);
            RemovePeer(peer);
        }

        private void OnReceiveData(NetPeer peer, NetDataReader reader)
        {
            try
            {
                MessageType messageType = (MessageType) reader.GetByte();

                if (messageType != MessageType.ClientHandshake && pendingConnections.Any(conn => conn.Peer == peer))
                {
                    DisconnectPeer(peer, DisconnectReason.NotAccepted);
                }

                switch (messageType)
                {
                    default: throw new UnexpectedMessageFromClientException(messageType);
                    case MessageType.ClientHandshake:
                    {
                        if (Players.ContainsKey(peer))
                        {
                            DisconnectPeer(peer, DisconnectReason.DuplicateHandshake);
                            break;
                        }

                        int version = reader.GetInt();
                        string playerName = reader.GetString();
                        PlayerMove movementData = reader.GetPlayerMove();

                        if (version != SharedConstants.Version)
                        {
                            DisconnectPeer(peer, version < SharedConstants.Version ? DisconnectReason.VersionOlder : DisconnectReason.VersionNewer);
                            break;
                        }

                        if (playerName.Length > SharedConstants.MaxNameLength || !playerName.All(ch => SharedConstants.AllowedCharacters.Contains(ch)))
                        {
                            DisconnectPeer(peer, DisconnectReason.InvalidName);
                            break;
                        }

                        pendingConnections.RemoveAll(conn => conn.Peer == peer);
                        Console.WriteLine($"Got valid handshake from {peer.EndPoint}");

                        var player = AddPeer(peer, playerName);
                        Players[peer].Movement = movementData;
                        
                        var writer = new NetDataWriter();
                        writer.Put(MessageType.CreatePlayer);
                        writer.Put(player.Id);
                        writer.Put(player.Name);
                        writer.Put(player.Movement);
                        Broadcast(writer, SendOptions.ReliableOrdered, peer);

                        Console.WriteLine($"Peer with id {player.Id} is now spawned");

                        break;
                    }
                    case MessageType.MoveData:
                    {
                        NetPlayer player = Players[peer];
                        player.Movement = reader.GetPlayerMove();
                        break;
                    }
                    case MessageType.ChatMessage:
                    {
                        var message = reader.GetString();

                        message = message.Trim();

                        if (message.Length > SharedConstants.MaxChatLength)
                            message = message.Substring(0, SharedConstants.MaxChatLength);

                        Color color = Color.white;

                        var writer = new NetDataWriter();
                        writer.Put(MessageType.ChatMessage);
                        writer.Put(Players[peer].Id);
                        writer.Put(color);
                        writer.Put(message);

                        Broadcast(writer, SendOptions.ReliableOrdered);
                        break;
                    }
                }
            }
            catch (UnexpectedMessageFromClientException ex)
            {
                Console.WriteLine($"Peer sent unexpected message type: {ex.MessageType}");
                DisconnectPeer(peer, DisconnectReason.InvalidMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("OnReceiveData errored:\n" + ex);
                DisconnectPeer(peer, DisconnectReason.InvalidMessage);
            }
        }
    }
}
