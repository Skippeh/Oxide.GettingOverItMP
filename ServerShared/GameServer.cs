using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using Facepunch.Steamworks;
using Lidgren.Network;
using ServerShared.Networking;
using ServerShared.Player;
using Color = UnityEngine.Color;

namespace ServerShared
{
    public class GameServer
    {
        class PendingConnection
        {
            public readonly NetConnection Client;
            public readonly ulong SteamId;
            public string PlayerName;
            public PlayerMove Movement;

            public PendingConnection(NetConnection client, ulong steamId, string playerName, PlayerMove movement)
            {
                Client = client;
                SteamId = steamId;
                PlayerName = playerName;
                Movement = movement;
            }
        }

        public static readonly TimeSpan PendingConnectionTimeout = new TimeSpan(0, 0, 0, 5); // 5 seconds

        public string Name;
        public int Port => server.Configuration.Port;
        public int MaxPlayers => server.Configuration.MaximumConnections;
        public Facepunch.Steamworks.Server SteamServer { get; private set; }

        public readonly bool ListenServer;
        public readonly bool PrivateServer;
        public readonly bool RequireSteamAuth;

        public readonly Dictionary<NetConnection, NetPlayer> Players = new Dictionary<NetConnection, NetPlayer>();

        private readonly GameServerPeer server;

        private double nextSendTime = 0;
        private readonly List<PendingConnection> pendingConnections = new List<PendingConnection>();

        public GameServer(string name, int maxConnections, int port, bool listenServer, bool privateServer, bool requireSteamAuth)
        {
            if (maxConnections <= 0)
                throw new ArgumentException("Max connections needs to be > 0.");

            var config = new NetPeerConfiguration(SharedConstants.AppName)
            {
                MaximumConnections = maxConnections,
                Port = port,
                ConnectionTimeout = 5,
                PingInterval = 1f,
                EnableUPnP = true
            };

            config.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);

            server = new GameServerPeer(config);

            server.Connected += OnConnectionConnected;
            server.Disconnected += OnConnectionDisconnected;
            server.DataReceived += OnReceiveData;
            server.DiscoveryRequest += OnDiscoveryRequest;

            ListenServer = listenServer;
            PrivateServer = privateServer;
            Name = name;
            RequireSteamAuth = requireSteamAuth;

            if (listenServer)
            {
                // Todo: Implement NAT punchthrough.
            }
        }

        public void Start()
        {
            server.Start();

            if (!PrivateServer)
            {
                MasterServer.Start(this);
            }

            var serverInit = new ServerInit("Getting Over It", "Getting Over It with Bennett Foddy")
            {
                GamePort = (ushort) Port,
                Secure = false
            };

            if (RequireSteamAuth)
            {
                SteamServer = new Server(SharedConstants.SteamAppId, serverInit);
                SteamServer.Auth.OnAuthChange += OnSteamAuthChange;
                SteamServer.ServerName = "Testy server";
                SteamServer.AutomaticHeartbeats = false;
                SteamServer.DedicatedServer = !ListenServer;
                SteamServer.MaxPlayers = MaxPlayers;
                SteamServer.LogOnAnonymous();

                Console.WriteLine("Steam authentication enabled.");
            }
        }

        public void Stop()
        {
            if (RequireSteamAuth)
            {
                foreach (var player in Players.Values)
                {
                    SteamServer.Auth.EndSession(player.SteamId);
                }

                SteamServer?.Dispose();
                SteamServer = null;
            }

            server.Shutdown("bye");

            if (!PrivateServer)
            {
                MasterServer.Stop();
            }
        }

        public void Update()
        {
            server.Update();
            SteamServer?.Update();

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

        private NetPlayer AddConnection(NetConnection connection, string playerName, ulong steamId)
        {
            var netPlayer = new NetPlayer(connection, playerName, this, steamId);
            Players[connection] = netPlayer;

            netPlayer.Name = $"[{netPlayer.Id}] {netPlayer.Name}"; // Prefix name with ID.

            var netMessage = server.CreateMessage();
            netMessage.Write(MessageType.HandshakeResponse);
            netMessage.Write(netPlayer.Id);
            netMessage.Write(netPlayer.Name);
            netMessage.Write(netPlayer.Wins);

            var allPlayers = Players.Values.Where(plr => !plr.Spectating && plr.Peer != connection).ToList();
            var allNames = allPlayers.ToDictionary(plr => plr.Id, plr => plr.Name);
            var allWins = allPlayers.ToDictionary(plr => plr.Id, plr => plr.Wins);
            var allPlayersDict = allPlayers.ToDictionary(plr => plr.Id, plr => plr.Movement);
            netMessage.Write(allNames);
            netMessage.Write(allWins);
            netMessage.Write(allPlayersDict);

            var serverInfo = GetServerInfo();
            netMessage.Write(serverInfo);

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
            Console.WriteLine($"Incoming from {args.Connection.RemoteEndPoint}");

            try
            {
                // Todo: check ip ban

                NetIncomingMessage hailMessage = args.Connection.RemoteHailMessage;
                int version = hailMessage.ReadInt32();

                if (version != SharedConstants.Version)
                {
                    KickConnection(args.Connection, version < SharedConstants.Version ? DisconnectReason.VersionOlder : DisconnectReason.VersionNewer);
                    return;
                }

                string playerName = hailMessage.ReadString().Trim();
                PlayerMove movementData = hailMessage.ReadPlayerMove();

                if (playerName.Length == 0 || playerName.Length > SharedConstants.MaxNameLength || !playerName.All(ch => SharedConstants.AllowedCharacters.Contains(ch)))
                {
                    KickConnection(args.Connection, DisconnectReason.InvalidName);
                    return;
                }

                ulong steamId = 0; // Unverified steam id
                byte[] sessionData = null;
                bool hasAuth = hailMessage.ReadBoolean();
                int numWins = hasAuth ? 0 : hailMessage.ReadInt32();

                if (hasAuth)
                {
                    int sessionLength = hailMessage.ReadInt32();
                    sessionData = hailMessage.ReadBytes(sessionLength);
                    steamId = hailMessage.ReadUInt64();
                }

                if (RequireSteamAuth)
                {
                    if (!hasAuth)
                        throw new Exception("No steam auth session ticket in hail message.");

                    Console.WriteLine($"{steamId} - {sessionData.Length}");

                    if (!SteamServer.Auth.StartSession(sessionData, steamId))
                    {
                        throw new Exception("StartSession returned false");
                    }

                    pendingConnections.Add(new PendingConnection(args.Connection, steamId, playerName, movementData));
                    Console.WriteLine($"Connection from {args.Connection.RemoteEndPoint}, awaiting steam auth approval...");
                }
                else
                {
                    AcceptConnection(args.Connection, playerName, movementData, 0, numWins);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                KickConnection(args.Connection, DisconnectReason.InvalidSteamSession);
                return;
            }
        }

        private void OnSteamAuthChange(ulong steamId, ulong ownerId, ServerAuth.Status status)
        {
            var pendingConnection = pendingConnections.FirstOrDefault(conn => conn.SteamId == steamId);
            var connection = pendingConnection?.Client ?? Players.FirstOrDefault(plr => plr.Value.SteamId == ownerId).Key;

            if (steamId != ownerId)
            {
                KickConnection(connection, DisconnectReason.InvalidSteamSession);
                return;
            }

            if (status == ServerAuth.Status.OK)
            {
                if (pendingConnection != null)
                {
                    // Todo: check steamid ban

                    pendingConnections.Remove(pendingConnection);
                    Console.WriteLine($"Got valid steam auth from {connection.RemoteEndPoint}");
                    AcceptConnection(connection, pendingConnection.PlayerName, pendingConnection.Movement, pendingConnection.SteamId, 0);

                    SteamServer.Stats.Refresh(ownerId, (ownerId2, success) =>
                    {
                        if (!success)
                        {
                            Console.WriteLine($"Failed to refresh stats for steamid {ownerId2}.");
                            return;
                        }

                        int totalWins = SteamServer.Stats.GetInt(ownerId2, "wins");
                        Players[connection].Wins = totalWins;
                        float goldness = totalWins / 50f;
                        Players[connection].SetGoldness(goldness * goldness);
                    });
                }
            }
            else
            {
                KickConnection(connection, DisconnectReason.InvalidSteamSession);
            }
        }

        private void AcceptConnection(NetConnection connection, string playerName, PlayerMove movementData, ulong steamId, int wins)
        {
            var player = AddConnection(connection, playerName, steamId);
            player.Movement = movementData;
            player.Wins = wins;

            var writer = server.CreateMessage();
            writer.Write(MessageType.CreatePlayer);
            writer.Write(player.Id);
            writer.Write(player.Name);
            writer.Write(player.Movement);
            writer.Write(player.Wins);
            Broadcast(writer, NetDeliveryMethod.ReliableOrdered, 0, connection);

            Console.WriteLine($"Client with id {player.Id} is now spawned");
            BroadcastChatMessage($"{player.Name} joined the server.", SharedConstants.ColorBlue, connection);
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

            if (RequireSteamAuth)
            {
                SteamServer.Auth.EndSession(player.SteamId);
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
                if (pendingConnections.Any(conn => conn.Client == connection))
                {
                    KickConnection(connection, DisconnectReason.NotAccepted);
                    return;
                }

                NetPlayer peerPlayer = Players.ContainsKey(connection) ? Players[connection] : null;

                switch (messageType)
                {
                    default: throw new UnexpectedMessageFromClientException(messageType);
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

        private void OnDiscoveryRequest(object sender, NetIncomingMessage message)
        {
            var toSend = server.CreateMessage();
            var serverInfo = GetServerInfo();

            toSend.Write(serverInfo);
            server.SendDiscoveryResponse(toSend, message.SenderEndPoint);
        }

        private DiscoveryServerInfo GetServerInfo()
        {
            return new DiscoveryServerInfo
            {
                Name = Name,
                Players = (ushort) Players.Count,
                MaxPlayers = (ushort) server.Configuration.MaximumConnections
            };
        }
    }
}
