using System.Collections.Generic;

namespace ServerShared.Networking
{
    public class ServerInfo
    {
        public string Name;
        public int Players;
        public int MaxPlayers;
        public float Ping;
        public int ServerVersion;
        public List<string> PlayerNames = new List<string>();

        public string Ip;
        public int Port;
    }

    public class MasterServerInfo
    {
        public string Ip;
        public int Port;

        public MasterServerInfo(string ip, int port)
        {
            Ip = ip;
            Port = port;
        }
    }
}
