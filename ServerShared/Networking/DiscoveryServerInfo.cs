using System.Collections.Generic;

namespace ServerShared.Networking
{
    public class DiscoveryServerInfo
    {
        public const int Version = 0;

        public string Name;
        public ushort Players;
        public ushort MaxPlayers;
        public List<string> PlayerNames = new List<string>();
        public int ServerVersion = 7;
    }
}
