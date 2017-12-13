using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using ServerShared.Player;
using UnityEngine;

namespace ServerShared
{
    public class GameServer
    {
        class PendingConnection
        {
            public readonly NetConnection Client;
            public readonly DateTime JoinTime;

            public PendingConnection(NetConnection client, DateTime joinTime)
            {
                Client = client;
                JoinTime = joinTime;
            }
        }

        public static readonly TimeSpan PendingConnectionTimeout = new TimeSpan(0, 0, 0, 5); // 5 seconds

        public int Port => server.Configuration.Port;
        public readonly bool ListenServer;

        public readonly Dictionary<NetConnection, NetPlayer> Players = new Dictionary<NetConnection, NetPlayer>();
        
        private readonly GameServerPeer server;

        private double nextSendTime = 0;
        private readonly List<PendingConnection> pendingConnections = new List<PendingConnection>();

        public GameServer(int maxConnections, int port, bool listenServer)
        {
            if (maxConnections <= 0)
                throw new ArgumentException("Max connections needs to be > 0.");
            
            server = new GameServerPeer(new NetPeerConfiguration(SharedConstants.AppName)
            {
                MaximumConnections = maxConnections,
                Port = port,
                ConnectionTimeout = 5,
                PingInterval = 1f,
                EnableUPnP = true
            });
            
            server.Connected += OnConnectionConnected;
            server.Disconnected += OnConnectionDisconnected;
            server.DataReceived += OnReceiveData;

            ListenServer = listenServer;
            
            if (listenServer)
            {
                // Todo: Implement NAT punchthrough.
            }
        }

        public void Start()
        {
            server.Start();
        }

        public void Stop()
        {
            server.Shutdown("bye");
        }
        
        public void Update()
        {
            server.Update();

            // Disconnect timed out pending connections
            foreach (var connection in pendingConnections.ToList())
            {
                if (DateTime.UtcNow - connection.JoinTime >= PendingConnectionTimeout)
                {
                    Console.WriteLine("Disconnecting pending connection (handshake timeout)");
                    KickConnection(connection.Client, DisconnectReason.HandshakeTimeout);
                }
            }

            double ms = DateTime.UtcNow.Ticks / 10_000d;
            
            if (ms >= nextSendTime)
            {
                nextSendTime = ms + 1000f / SharedConstants.UpdateRate;

                if (Players.Count <= 0)
                    return;

                Dictionary<int, PlayerMove> toSend = Players.Values.Where(plr => !plr.Spectating).ToDictionary(plr => plr.Id, plr => plr.Movement);

                var writer = server.CreateMessage();
                writer.Write(MessageType.MoveData);
                writer.Write(toSend);

                Broadcast(writer, NetDeliveryMethod.UnreliableSequenced, SharedConstants.MoveDataChannel);
            }
        }

        public void BroadcastChatMessage(string message, NetConnection except = null)
        {
            BroadcastChatMessage(message, Color.white, except);
        }

        public void BroadcastChatMessage(string message, Color color, NetConnection except = null)
        {
            BroadcastChatMessage(message, color, null, except);
        }

        public void BroadcastChatMessage(string message, Color color, NetPlayer player, NetConnection except = null)
        {
            var netMessage = server.CreateMessage();
            netMessage.Write(MessageType.ChatMessage);
            netMessage.Write(player?.Name);
            netMessage.WriteRgbaColor(color);
            netMessage.Write(message);
            netMessage.Write(netMessage);
            
            Broadcast(netMessage, NetDeliveryMethod.ReliableOrdered, 0, except);
        }

        public NetOutgoingMessage CreateMessage()
        {
            return server.CreateMessage();
        }

        private NetPlayer AddConnection(NetConnection connection, string playerName)
        {
            var netPlayer = new NetPlayer(connection, playerName, this);
            Players[connection] = netPlayer;

            var netMessage = server.CreateMessage();
            netMessage.Write(MessageType.HandshakeResponse);
            netMessage.Write(netPlayer.Id);
            netMessage.Write(netPlayer.Name);

            var allPlayers = Players.Values.Where(plr => !plr.Spectating && plr.Peer != connection).ToList();
            var allNames = allPlayers.ToDictionary(plr => plr.Id, plr => plr.Name);
            var allPlayersDict = allPlayers.ToDictionary(plr => plr.Id, plr => plr.Movement);
            netMessage.Write(allNames);
            netMessage.Write(allPlayersDict);
            connection.SendMessage(netMessage, NetDeliveryMethod.ReliableOrdered, 0);

            Console.WriteLine($"Added client from {connection.RemoteEndPoint} with id {netPlayer.Id} (total: {Players.Count})");
            return netPlayer;
        }

        private void RemoveConnection(NetConnection connection)
        {
            if (!Players.ContainsKey(connection))
                return;

            int playerId = Players[connection].Id;

            Players.Remove(connection);

            var writer = server.CreateMessage();
            writer.Write(MessageType.RemovePlayer);
            writer.Write(playerId);
            Broadcast(writer, NetDeliveryMethod.ReliableOrdered, 0);

            Console.WriteLine($"Removed client from {connection.RemoteEndPoint} with id {playerId} (total: {Players.Count})");
        }

        /// <summary>
        /// Sends a message to all spawned clients.
        /// </summary>
        public void Broadcast(NetOutgoingMessage message, NetDeliveryMethod method, int sequenceChannel, NetConnection except = null)
        {
            var sendTargets = Players.Where(kv => except == null || kv.Key != except).Select(kv => kv.Key).ToList();

            if (sendTargets.Count > 0)
                server.SendMessage(message, sendTargets, method, sequenceChannel);
        }

        private void KickConnection(NetConnection connection, DisconnectReason reason)
        {
            Console.WriteLine($"Disconnecting client from {connection.RemoteEndPoint}: {reason}");
            connection.Disconnect(reason);
        }

        private void OnConnectionConnected(object sender, ConnectedEventArgs args)
        {
            // Todo: protect against mass connections from the same ip.

            Console.WriteLine($"Connection from {args.Connection.RemoteEndPoint}");
            pendingConnections.Add(new PendingConnection(args.Connection, DateTime.UtcNow));
        }

        private void OnConnectionDisconnected(object sender, DisconnectedEventArgs args)
        {
            var connection = args.Connection;
            pendingConnections.RemoveAll(conn => conn.Client == connection);

            NetPlayer player;

            if (Players.ContainsKey(connection))
                player = Players[connection];
            else
                return;

            foreach (var netPlayer in Players.Values)
            {
                if (netPlayer.SpectateTarget == player)
                {
                    netPlayer.Spectate(null);
                }
            }

            RemoveConnection(connection);
            BroadcastChatMessage($"{player.Name} left the server.", SharedConstants.ColorBlue);
        }

        private void OnReceiveData(object sender, DataReceivedEventArgs args)
        {
            var connection = args.Connection;
            var netMessage = args.Message;
            MessageType messageType = args.MessageType;

            try
            {
                if (messageType != MessageType.ClientHandshake && pendingConnections.Any(conn => conn.Client == connection))
                {
                    KickConnection(connection, DisconnectReason.NotAccepted);
                    return;
                }

                NetPlayer peerPlayer = Players.ContainsKey(connection) ? Players[connection] : null;

                switch (messageType)
                {
                    default: throw new UnexpectedMessageFromClientException(messageType);
                    case MessageType.ClientHandshake:
                    {
                        if (Players.ContainsKey(connection))
                        {
                            KickConnection(connection, DisconnectReason.DuplicateHandshake);
                            break;
                        }

                        int version = netMessage.ReadInt32();
                        string playerName = netMessage.ReadString().Trim();
                        PlayerMove movementData = netMessage.ReadPlayerMove();

                        if (version != SharedConstants.Version)
                        {
                            KickConnection(connection, version < SharedConstants.Version ? DisconnectReason.VersionOlder : DisconnectReason.VersionNewer);
                            break;
                        }

                        if (playerName.Length == 0 || playerName.Length > SharedConstants.MaxNameLength || !playerName.All(ch => SharedConstants.AllowedCharacters.Contains(ch)))
                        {
                            KickConnection(connection, DisconnectReason.InvalidName);
                            break;
                        }
                        
                        pendingConnections.RemoveAll(conn => conn.Client == connection);
                        Console.WriteLine($"Got valid handshake from {connection.RemoteEndPoint}");

                        var player = AddConnection(connection, playerName);
                        Players[connection].Movement = movementData;

                        var writer = server.CreateMessage();
                        writer.Write(MessageType.CreatePlayer);
                        writer.Write(player.Id);
                        writer.Write(player.Name);
                        writer.Write(player.Movement);
                        Broadcast(writer, NetDeliveryMethod.ReliableOrdered, 0, connection);
                        
                        Console.WriteLine($"Client with id {player.Id} is now spawned");
                        BroadcastChatMessage($"{player.Name} joined the server.", SharedConstants.ColorBlue, connection);
                        
                        break;
                    }
                    case MessageType.MoveData:
                    {
                        peerPlayer.Movement = netMessage.ReadPlayerMove();
                        break;
                    }
                    case MessageType.ChatMessage:
                    {
                        var message = netMessage.ReadString();

                        message = message.Trim();

                        if (message.Length > SharedConstants.MaxChatLength)
                            message = message.Substring(0, SharedConstants.MaxChatLength);

                        // Todo: better handling of chat commands
                        if (message.StartsWith("/"))
                        {
                            if (message.StartsWith("/spectate "))
                            {
                                NetPlayer target;
                                int targetId;
                                if (!int.TryParse(message.Substring("/spectate ".Length), out targetId))
                                {
                                    var players = Players.Values.Where(plr => !plr.Spectating && plr.Name.ToLower().StartsWith(message.Substring("/spectate ".Length).ToLower())).ToList();

                                    if (players.Count == 0)
                                    {
                                        peerPlayer.SendChatMessage("There is no player with this name.", SharedConstants.ColorRed);
                                        return;
                                    }

                                    if (players.Count > 1)
                                    {
                                        peerPlayer.SendChatMessage("Found more than 1 player with this name. Try be more specific or type their id instead.", SharedConstants.ColorRed);
                                        return;
                                    }

                                    target = players.First();
                                }
                                else
                                {
                                    target = Players.Values.FirstOrDefault(plr => !plr.Spectating && plr.Id == targetId);

                                    if (target == null)
                                    {
                                        peerPlayer.SendChatMessage("There is no player with this id.", SharedConstants.ColorRed);
                                        return;
                                    }
                                }

                                if (target == peerPlayer)
                                {
                                    peerPlayer.SendChatMessage("You can't spectate yourself dummy.", SharedConstants.ColorRed);
                                    return;
                                }

                                peerPlayer.Spectate(target);
                            }
                            else if (message.StartsWith("/shrug"))
                            {
                                string prefix = "";

                                if (message.Length > "/shrug ".Length)
                                    prefix = message.Substring("/shrug ".Length);
                                
                                prefix = prefix.Trim();

                                BroadcastChatMessage($"{prefix} ¯\\_(ツ)_/¯", Color.white, peerPlayer);
                            }
                            else if (message.StartsWith("/tableflip"))
                            {
                                string prefix = "";

                                if (message.Length > "/tableflip ".Length)
                                    prefix = message.Substring("/tableflip ".Length);

                                prefix = prefix.Trim();

                                BroadcastChatMessage($"{prefix} (╯°□°）╯︵ ┻━┻", Color.white, peerPlayer);
                            }

                            return;
                        }

                        Color color = Color.white;
                        BroadcastChatMessage(message, color, peerPlayer);

                        break;
                    }
                    case MessageType.ClientStopSpectating:
                    {
                        peerPlayer.Spectate(null);
                        break;
                    }
                    case MessageType.SpectateTarget:
                    {
                        int targetId = netMessage.ReadInt32();
                        NetPlayer targetPlayer = Players.Values.FirstOrDefault(plr => !plr.Spectating && plr.Id == targetId);
                            
                        if (targetPlayer != null)
                            peerPlayer.Spectate(targetPlayer);
                        
                        break;
                    }
                }
            }
            catch (UnexpectedMessageFromClientException ex)
            {
                Console.WriteLine($"Client sent unexpected message type: {ex.MessageType}");
                KickConnection(connection, DisconnectReason.InvalidMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine("OnReceiveData errored:\n" + ex);
                KickConnection(connection, DisconnectReason.InvalidMessage);
            }
        }
    }
}
