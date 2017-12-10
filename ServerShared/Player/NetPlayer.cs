using System;
using JetBrains.Annotations;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace ServerShared.Player
{
    public class NetPlayer
    {
        public readonly int Id;
        public readonly NetPeer Peer;

        public PlayerMove Movement;
        public string Name;
        public NetPlayer SpectateTarget;
        public bool Spectating => SpectateTarget != null;

        private GameServer server;

        private static int idCounter = 1;

        public NetPlayer(NetPeer peer, string name, [NotNull] GameServer server)
        {
            Peer = peer ?? throw new ArgumentNullException(nameof(peer));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            Id = idCounter++;
        }
        
        public void SendChatMessage(string message, Color color)
        {
            var writer = new NetDataWriter();
            writer.Put(MessageType.ChatMessage);
            writer.Put((string) null);
            writer.Put(color);
            writer.Put(message);

            Peer.Send(writer, SendOptions.ReliableOrdered);
        }

        public void Spectate(NetPlayer target)
        {
            if (SpectateTarget == target)
                return;

            bool wasSpectating = SpectateTarget != null;
            SpectateTarget = target;

            // Send message to peer to start/stop spectating target
            var writer = new NetDataWriter();
            writer.Put(MessageType.SpectateTarget);
            writer.Put(target?.Id ?? 0);

            Peer.Send(writer, SendOptions.ReliableOrdered);

            writer.Reset();

            if (target != null && !wasSpectating) // Started spectating
            {
                // Stop people from spectating this player
                foreach (var netPlayer in server.Players.Values)
                {
                    if (netPlayer.SpectateTarget == this)
                        netPlayer.Spectate(null);
                }

                // Broadcast message to all other peers to remove this peers player (if he wasn't spectating already, otherwise he would already be despawned)
                writer.Put(MessageType.RemovePlayer);
                writer.Put(Id);

                server.Broadcast(writer, SendOptions.ReliableOrdered, Peer);
            }
            else if (target == null) // Stopped spectating
            {
                // Broadcast message to all other peers to add this peers player
                writer.Put(MessageType.CreatePlayer);
                writer.Put(Id);
                writer.Put(Name);
                writer.Put(Movement);
                server.Broadcast(writer, SendOptions.ReliableOrdered, Peer);
            }
        }
    }
}
