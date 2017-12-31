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
    }
}
