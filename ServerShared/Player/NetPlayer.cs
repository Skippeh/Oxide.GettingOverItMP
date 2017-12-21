using System;
using Lidgren.Network;
using UnityEngine;

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
        public int Wins = 0;

        private GameServer server;

        private static int idCounter = 1;

        public NetPlayer(NetConnection connection, string name, GameServer server, ulong steamId)
        {
            Peer = connection ?? throw new ArgumentNullException(nameof(connection));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SteamId = steamId;
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            Id = idCounter++;
        }
        
        public void SendChatMessage(string message, Color color)
        {
            var writer = server.CreateMessage();
            writer.Write(MessageType.ChatMessage);
            writer.Write((string) null);
            writer.WriteRgbaColor(color);
            writer.Write(message);

            Peer.SendMessage(writer, NetDeliveryMethod.ReliableOrdered, 0);
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
                server.Broadcast(netMessage, NetDeliveryMethod.ReliableOrdered, 0, Peer);
            }
        }

        public void SetGoldness(float goldness)
        {
            goldness = Mathf.Clamp(goldness, 0f, 2f);

            var message = server.CreateMessage();
            message.Write(MessageType.PlayerGoldness);
            message.Write(Id);
            message.Write(goldness);

            server.Broadcast(message, NetDeliveryMethod.ReliableOrdered, 0);
        }
    }
}
