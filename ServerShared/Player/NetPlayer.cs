using System;
using LiteNetLib;

namespace ServerShared.Player
{
    public class NetPlayer
    {
        public readonly int Id;
        public readonly NetPeer Peer;

        public PlayerMove Movement;
        public string Name;

        private static int idCounter = 1;

        public NetPlayer(NetPeer peer, string name)
        {
            Peer = peer ?? throw new ArgumentNullException(nameof(peer));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Id = idCounter++;
        }
    }
}
