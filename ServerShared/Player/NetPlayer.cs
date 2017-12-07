using System;
using JetBrains.Annotations;
using LiteNetLib;

namespace ServerShared.Player
{
    public class NetPlayer
    {
        public readonly int Id;
        public readonly NetPeer Peer;

        private static int idCounter = 1;

        public NetPlayer([NotNull] NetPeer peer)
        {
            Peer = peer ?? throw new ArgumentNullException(nameof(peer));
            Id = idCounter++;
        }
    }
}
