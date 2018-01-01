using System;
using System.Net;
using Newtonsoft.Json;

namespace ServerShared.Player
{
    public enum IdentityType
    {
        SteamId,
        Ip
    }

    public class PlayerIdentity
    {
        public IdentityType Type;
        public ulong SteamId;
        public uint Ip;

        public PlayerIdentity(ulong steamId)
        {
            Type = IdentityType.SteamId;
            SteamId = steamId;
        }

        public PlayerIdentity(uint ip)
        {
            Type = IdentityType.Ip;
            Ip = ip;
        }

        [JsonConstructor]
        protected PlayerIdentity() { }

        public bool Matches(NetPlayer player)
        {
            switch (Type)
            {
                case IdentityType.Ip:
                    return player.Peer.RemoteEndPoint.Address.ToUint32() == Ip;
                case IdentityType.SteamId:
                    return player.SteamId == SteamId;
            }

            throw new NotImplementedException("Unimplemented IdentityType");
        }

        public override string ToString()
        {
            switch (Type)
            {
                case IdentityType.Ip:
                    return $"IP: {new IPAddress(Ip)}";
                case IdentityType.SteamId:
                    return $"SteamID64: {SteamId}";
            }

            throw new NotImplementedException($"IdentityType not implemented: {Type}");
        }
    }

    public class PlayerAccessLevelIdentity : PlayerIdentity
    {
        public AccessLevel AccessLevel;

        public PlayerAccessLevelIdentity(ulong steamId, AccessLevel accessLevel) : base(steamId)
        {
            AccessLevel = accessLevel;
        }

        public PlayerAccessLevelIdentity(uint ip, AccessLevel accessLevel) : base(ip)
        {
            AccessLevel = accessLevel;
        }

        [JsonConstructor]
        private PlayerAccessLevelIdentity() { }
    }
}
