﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using Facepunch.Steamworks;
using Lidgren.Network;
using ServerShared.Logging;
using ServerShared.Networking;
using ServerShared.Player;
using Color = UnityEngine.Color;

namespace ServerShared
{
    public class GameServer
    {
        public string Name;
        public int Port => server.Configuration.Port;
        public int MaxPlayers => server.Configuration.MaximumConnections;
        public Facepunch.Steamworks.Server SteamServer { get; private set; }
        public readonly ChatCommandManager ChatCommands;
        public readonly ConsoleCommandManager ConsoleCommands;
        public List<PlayerBan> BannedPlayers => Config.Bans;
        public ServerConfig Config { get; private set; }
        public bool Running { get; private set; }

        public readonly bool ListenServer;
        public readonly bool PrivateServer;
        public readonly bool RequireSteamAuth;
        public readonly string ConfigDirectory;

        public readonly Dictionary<NetConnection, NetPlayer> Players = new Dictionary<NetConnection, NetPlayer>();
        private readonly List<PendingConnection> pendingConnections = new List<PendingConnection>();
        
        private readonly GameServerPeer server;

        private double nextSendTime = 0;

        public GameServer(string name, int maxConnections, int port, bool listenServer, bool privateServer, bool requireSteamAuth, string configDirectory)
        {
            if (maxConnections <= 0)
                throw new ArgumentException("Max connections needs to be > 0.");

            var peerConfig = new NetPeerConfiguration(SharedConstants.AppName)
            {
                MaximumConnections = maxConnections,
                Port = port,
                ConnectionTimeout = 5,
                PingInterval = 1f,
                EnableUPnP = true
            };

            peerConfig.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);

            server = new GameServerPeer(peerConfig);

            server.Connected += OnConnectionConnected;
            server.Disconnected += OnConnectionDisconnected;
            server.DataReceived += OnReceiveData;
            server.DiscoveryRequest += OnDiscoveryRequest;

            ListenServer = listenServer;
            PrivateServer = privateServer;
            Name = name;
            RequireSteamAuth = requireSteamAuth;
            var callingAssembly = Assembly.GetCallingAssembly();
            ChatCommands = new ChatCommandManager(this, callingAssembly);
            ConsoleCommands = new ConsoleCommandManager(this, callingAssembly);
            ConfigDirectory = configDirectory;

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
            
            if (ServerConfig.LoadConfig(ConfigDirectory, out var config))
            {
                Config = config;
                Logger.LogDebug($"Loaded {Config.Bans.Count} bans.");
            }
            
            if (RequireSteamAuth)
            {
                Logger.LogDebug("Enabling steam authentication...");

                var serverInit = new ServerInit("Getting Over It", "Getting Over It with Bennett Foddy")
                {
                    GamePort = (ushort)Port,
                    Secure = false
                };

                SteamServer = new Server(SharedConstants.SteamAppId, serverInit);
                SteamServer.Auth.OnAuthChange += OnSteamAuthChange;
                SteamServer.ServerName = Name;
                SteamServer.AutomaticHeartbeats = false;
                SteamServer.DedicatedServer = !ListenServer;
                SteamServer.MaxPlayers = MaxPlayers;
                SteamServer.LogOnAnonymous();

                Logger.LogDebug("Steam authentication enabled.");
            }

            Running = true;
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

            Running = false;
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
            BroadcastChatMessage(message, color, player?.Id ?? 0, player?.Name, except);
        }

        public void BroadcastChatMessage(string message, Color color, int playerId, string playerName, NetConnection except = null)
        {
            var netMessage = server.CreateMessage();
            netMessage.Write(MessageType.ChatMessage);
            netMessage.Write(playerId);
            netMessage.Write(playerName);
            netMessage.WriteRgbaColor(color);
            netMessage.Write(message);
            netMessage.Write(netMessage);

            Broadcast(netMessage, NetDeliveryMethod.ReliableOrdered, 0, except);

            string prefix = playerName == null ? "" : $"{playerName}: ";
            Logger.LogInfo($"[CHAT] {prefix}{message}");
        }

        public NetOutgoingMessage CreateMessage()
        {
            return server.CreateMessage();
        }

        public bool IpBanned(IPAddress ipAddress, out PlayerBan ban)
        {
            uint ip = GetUintIp(ipAddress);
            ban = BannedPlayers.FirstOrDefault(_ban => _ban.BanType == IdentityType.Ip && _ban.Ip == ip);
            return ban != null && !ban.Expired();
        }

        public bool SteamIdBanned(ulong steamId, out PlayerBan ban)
        {
            ban = BannedPlayers.FirstOrDefault(_ban => _ban.BanType == IdentityType.SteamId && _ban.SteamId == steamId);
            return ban != null && !ban.Expired();
        }

        public bool BanPlayer(NetPlayer player, string reason = null, DateTime? expirationDate = null)
        {
            if (SteamServer != null)
                BanSteamId(player.SteamId, reason, expirationDate, player.Name);
            else
                BanIp(player.Peer.RemoteEndPoint.Address, reason, expirationDate, player.Name);

            KickConnection(player.Peer, DisconnectReason.Banned, reason);
            return true;
        }

        public void BanIp(IPAddress ip, string reason = null, DateTime? expirationDate = null, string referenceName = null)
        {
            Config.RemoveExpiredBans();

            uint uintIp = GetUintIp(ip);
            if (IpBanned(ip, out var _))
                return;

            BannedPlayers.Add(new PlayerBan(uintIp, reason, expirationDate, referenceName));
            Config.Save();
        }

        public void BanSteamId(ulong steamId, string reason = null, DateTime? expirationDate = null, string referenceName = null)
        {
            Config.RemoveExpiredBans();

            if (SteamIdBanned(steamId, out var _))
                return;
            
            BannedPlayers.Add(new PlayerBan(steamId, reason, expirationDate, referenceName));
            Config.Save();
        }

        public bool UnbanIp(IPAddress ip)
        {
            var uintIp = GetUintIp(ip);
            bool success = BannedPlayers.RemoveAll(ban => ban.BanType == IdentityType.Ip && ban.Ip == uintIp) > 0;

            if (success)
                Config.Save();

            return success;
        }

        public bool UnbanSteamId(ulong steamId)
        {
            bool success = BannedPlayers.RemoveAll(ban => ban.BanType == IdentityType.SteamId && ban.SteamId == steamId) > 0;

            if (success)
                Config.Save();

            return success;
        }

        public bool UnbanByName(string name)
        {
            bool success = BannedPlayers.RemoveAll(ban => ban.ReferenceName.ToLower().StartsWith(name.ToLower())) > 0;

            if (success)
                Config.Save();

            return success;
        }
        
        public void RemoveBan(PlayerBan ban)
        {
            BannedPlayers.Remove(ban);
            Config.Save();
        }

        public IEnumerable<PlayerBan> FindBansByName(string name)
        {
            return BannedPlayers.Where(ban => ban.ReferenceName?.ToLower().StartsWith(name.ToLower()) == true);
        }

        public IEnumerable<NetPlayer> FindPlayers(string name, NameSearchOption searchOption)
        {
            string lowerName = name.ToLower();

            return Players.Values.Where(plr =>
            {
                switch (searchOption)
                {
                    case NameSearchOption.StartsWith:
                        return plr.Name.ToLower().StartsWith(lowerName);
                    case NameSearchOption.Contains:
                        return plr.Name.ToLower().Contains(lowerName);
                }

                throw new NotImplementedException($"NameSearchOption.{searchOption} not implemented.");
            });
        }

        public NetPlayer FindPlayer(string name, NameSearchOption searchOption)
        {
            return FindPlayers(name, searchOption).FirstOrDefault();
        }

        public NetPlayer FindPlayer(ulong steamId)
        {
            return Players.Values.FirstOrDefault(plr => plr.SteamId == steamId);
        }

        public NetPlayer FindPlayer(int id)
        {
            return Players.Values.FirstOrDefault(plr => plr.Id == id);
        }

        public NetPlayer FindPlayer(IPAddress ipAddress)
        {
            return Players.Values.FirstOrDefault(plr => plr.Peer.RemoteEndPoint.Address == ipAddress);
        }
        
        private NetPlayer AddConnection(NetConnection connection, string playerName, ulong steamId)
        {
            var netPlayer = new NetPlayer(connection, playerName, this, steamId);
            Players[connection] = netPlayer;
            
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

            Logger.LogDebug($"Added client from {connection.RemoteEndPoint} with id {netPlayer.Id} (total: {Players.Count})");
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

            Logger.LogDebug($"Removed client from {connection.RemoteEndPoint} with id {playerId} (total: {Players.Count})");
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

        public void KickConnection(NetConnection connection, DisconnectReason reason, string additionalInfo = null)
        {
            connection.Disconnect(reason, additionalInfo);
        }

        private void OnConnectionConnected(object sender, ConnectedEventArgs args)
        {
            Logger.LogDebug($"Incoming from {args.Connection.RemoteEndPoint}");

            try
            {
                if (IpBanned(args.Connection.RemoteEndPoint.Address, out var playerBan))
                {
                    KickConnection(args.Connection, DisconnectReason.Banned, playerBan.GetReasonWithExpiration());
                    return;
                }

                NetIncomingMessage hailMessage = args.Connection.RemoteHailMessage;
                int version = hailMessage.ReadInt32();

                if (version != SharedConstants.Version)
                {
                    KickConnection(args.Connection, version < SharedConstants.Version ? DisconnectReason.VersionOlder : DisconnectReason.VersionNewer);
                    return;
                }

                string playerName = hailMessage.ReadString().Trim();
                PlayerMove movementData = hailMessage.ReadPlayerMove();

                if (playerName.Length == 0 || playerName.Length > SharedConstants.MaxNameLength)
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
                    
                    if (!SteamServer.Auth.StartSession(sessionData, steamId))
                    {
                        throw new Exception("Could not start steam session.");
                    }

                    pendingConnections.Add(new PendingConnection(args.Connection, steamId, playerName, movementData));
                    Logger.LogDebug($"Connection from {args.Connection.RemoteEndPoint}, awaiting steam auth approval...");
                }
                else
                {
                    AcceptConnection(args.Connection, playerName, movementData, 0, numWins);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                KickConnection(args.Connection, DisconnectReason.InvalidSteamSession, ex.Message);
                return;
            }
        }

        private void OnSteamAuthChange(ulong steamId, ulong ownerId, ServerAuth.Status status)
        {
            var pendingConnection = pendingConnections.FirstOrDefault(conn => conn.SteamId == steamId);
            var connection = pendingConnection?.Client ?? Players.FirstOrDefault(plr => plr.Value.SteamId == ownerId).Key;

            if (connection == null || connection.Status != NetConnectionStatus.Connected) // Return if connection got kicked or disconnected between connecting and receiving steam auth status.
                return;

            if (steamId != ownerId)
            {
                KickConnection(connection, DisconnectReason.InvalidSteamSession, "Invalid steam id");
                return;
            }

            if (status == ServerAuth.Status.OK)
            {
                if (pendingConnection != null)
                {
                    if (SteamIdBanned(steamId, out var playerBan))
                    {
                        KickConnection(connection, DisconnectReason.Banned, playerBan.GetReasonWithExpiration());
                        return;
                    }

                    pendingConnections.Remove(pendingConnection);
                    Logger.LogDebug($"Got valid steam auth from {connection.RemoteEndPoint}");
                    AcceptConnection(connection, pendingConnection.PlayerName, pendingConnection.Movement, pendingConnection.SteamId, 0);

                    SteamServer.Stats.Refresh(ownerId, (ownerId2, success) =>
                    {
                        if (!success)
                        {
                            Logger.LogError($"Failed to refresh stats for steamid {ownerId2}.");
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
                KickConnection(connection, DisconnectReason.InvalidSteamSession, status.ToString());
            }
        }

        private void AcceptConnection(NetConnection connection, string playerName, PlayerMove movementData, ulong steamId, int wins)
        {
            var player = AddConnection(connection, playerName, steamId);
            player.Movement = movementData;
            player.Wins = wins;
            
            uint uintIp = GetUintIp(connection.RemoteEndPoint.Address);
            var accessLevelIdentity = Config.AccessLevels.FirstOrDefault(identity => (identity.Type == IdentityType.Ip && identity.Ip == uintIp) || (identity.Type == IdentityType.SteamId && identity.SteamId == steamId));

            if (accessLevelIdentity != null)
                player.SetAccessLevel(accessLevelIdentity.AccessLevel);

            var writer = server.CreateMessage();
            writer.Write(MessageType.CreatePlayer);
            writer.Write(player.Id);
            writer.Write(player.Name);
            writer.Write(player.Movement);
            writer.Write(player.Wins);
            Broadcast(writer, NetDeliveryMethod.ReliableOrdered, 0, connection);

            Logger.LogDebug($"Client with id {player.Id} is now spawned");
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

                NetPlayer peerPlayer = Players[connection];

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

                        if (ChatCommands.HandleMessage(peerPlayer, message))
                            return;
                        
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
                Logger.LogWarning($"Client sent unexpected message type: {ex.MessageType}");
                KickConnection(connection, DisconnectReason.InvalidMessage);
            }
            catch (Exception ex)
            {
                Logger.LogException("OnReceiveData threw an exception", ex);
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

        private static uint GetUintIp(IPAddress ipAddress)
        {
            byte[] ipBytes = ipAddress.GetAddressBytes();
            uint ip = ipBytes[0];
            ip += (uint) ipBytes[1] << 8;
            ip += (uint) ipBytes[2] << 16;
            ip += (uint) ipBytes[3] << 24;
            return ip;
        }
    }
}
