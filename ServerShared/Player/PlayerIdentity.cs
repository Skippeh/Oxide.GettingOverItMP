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
