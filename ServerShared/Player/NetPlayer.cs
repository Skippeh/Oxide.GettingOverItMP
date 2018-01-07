using System;
using Lidgren.Network;
using ServerShared.Logging;
using UnityEngine;
using Logger = ServerShared.Logging.Logger;

namespace ServerShared.Player
{
    public class NetPlayer
    {
        public readonly int Id;
        public readonly NetConnection Peer;
        public readonly ulong SteamId;

        public PlayerMove Movement;
        public string Name;
        public NetPlayer SpectateTarget;
        public bool Spectating => SpectateTarget != null;
        public int Wins { get; private set; } = 0;
        public float Goldness { get; private set; }
        public Color PotColor { get; private set; } = Color.white;
        public PlayerAccessLevelIdentity Identity { get; private set; }
        public AccessLevel AccessLevel => Identity.AccessLevel;

        private GameServer server;

        private static int idCounter = 1;

        public NetPlayer(NetConnection connection, string name, GameServer server, ulong steamId)
        {
            Peer = connection ?? throw new ArgumentNullException(nameof(connection));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SteamId = steamId;
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            Id = idCounter++;

            Identity = server.RequireSteamAuth
                ? new PlayerAccessLevelIdentity(steamId, AccessLevel.Player)
                : new PlayerAccessLevelIdentity(connection.RemoteEndPoint.Address.ToUint32(), AccessLevel.Player);
        }
        
        /// <param name="color">If null then Color.white will be used.</param>
        public void SendChatMessage(string message, UnityEngine.Color? color = null)
        {
            var writer = server.CreateMessage();
            writer.Write(MessageType.ChatMessage);
            writer.Write(0); // player id (0 = equivalent of null)
            writer.Write((string) null);
            writer.WriteRgbaColor(color ?? UnityEngine.Color.white);
            writer.Write(message);

            Peer.SendMessage(writer, NetDeliveryMethod.ReliableOrdered, 0);
        }

        public void SendConsoleMessage(string message, LogMessageType type)
        {
            // Todo: implement client console
        }

        public void Spectate(NetPlayer target)
        {
            if (SpectateTarget == target)
                return;

            bool wasSpectating = SpectateTarget != null;
            SpectateTarget = target;

            // Send message to connection to start/stop spectating target
            var netMessage = server.CreateMessage();
            netMessage.Write(MessageType.SpectateTarget);
            netMessage.Write(target?.Id ?? 0);

            Peer.SendMessage(netMessage, NetDeliveryMethod.ReliableOrdered, 0);

            netMessage = server.CreateMessage();

            if (target != null && !wasSpectating) // Started spectating
            {
                // Stop people from spectating this player
                foreach (var netPlayer in server.Players.Values)
                {
                    if (netPlayer.SpectateTarget == this)
                        netPlayer.Spectate(null);
                }

                // Broadcast message to all other peers to remove this peers player (if he wasn't spectating already, otherwise he would already be despawned)
                netMessage.Write(MessageType.RemovePlayer);
                netMessage.Write(Id);

                server.Broadcast(netMessage, NetDeliveryMethod.ReliableOrdered, 0, Peer);
            }
            else if (target == null) // Stopped spectating
            {
                // Broadcast message to all other peers to add this peers player
                netMessage.Write(MessageType.CreatePlayer);
                netMessage.Write(Id);
                netMessage.Write(Name);
                netMessage.Write(Movement);
                netMessage.Write(Wins);
                netMessage.Write(Goldness);
                netMessage.WriteRgbaColor(PotColor);
                server.Broadcast(netMessage, NetDeliveryMethod.ReliableOrdered, 0, Peer);
            }
        }

        public void SetPotProperties(float goldness, Color color, bool broadcastToClients = true)
        {
            Goldness = Mathf.Clamp01(goldness);
            PotColor = color;

            if (!broadcastToClients)
                return;

            BroadcastPotProperties();
        }

        public void SetGoldness(float goldness, bool broadcastToClients = true)
        {
            if (Math.Abs(Goldness - goldness) < 0.001f)
                return;

            Goldness = UnityEngine.Mathf.Clamp01(goldness);

            if (!broadcastToClients)
                return;

            BroadcastPotProperties();
        }

        public void SetPotColor(Color color, bool broadcastToClients = true)
        {
            PotColor = color;

            if (!broadcastToClients)
                return;

            BroadcastPotProperties();
        }

        public void SetWins(int wins, bool updateGoldness, bool broadcastToClients = true)
        {
            if (Wins == wins)
                return;

            Wins = wins;

            if (broadcastToClients)
            {
                var message = server.CreateMessage();
                message.Write(MessageType.PlayerWins);
                message.Write(Id);
                message.Write(Wins);

                server.Broadcast(message, NetDeliveryMethod.ReliableOrdered, 0);
            }

            if (updateGoldness)
                SetGoldness(Mathf.Pow(wins / 50f, 2), broadcastToClients);
        }

        public void SetAccessLevel(AccessLevel accessLevel)
        {
            Identity.AccessLevel = accessLevel;
            Logger.LogInfo($"{Name} access level set to {AccessLevel}.");
        }

        private void BroadcastPotProperties()
        {
            var message = server.CreateMessage();
            message.Write(MessageType.PlayerPotProperties);
            message.Write(Id);
            message.Write(Goldness);
            message.WriteRgbaColor(PotColor);

            server.Broadcast(message, NetDeliveryMethod.ReliableOrdered, 0);
        }
    }
}
