using System;
using ServerShared;

namespace Oxide.GettingOverItMP.Networking
{
    public static class ListenServer
    {
        public static GameServer Server { get; private set; }
        public static bool Running => Server != null;

        public static void Start(string name, int maxPlayers, int port, bool isPrivate, bool requireSteamAuth)
        {
            if (Running)
                throw new InvalidOperationException("The server is already running.");

            Server = new GameServer(name, maxPlayers, port, true, isPrivate, requireSteamAuth);
            Server.Start();
        }

        public static void Stop()
        {
            if (!Running)
                throw new InvalidOperationException("The server is not running.");

            Server.Stop();
            Server = null;
        }

        public static void Update()
        {
            if (Running)
                Server.Update();
        }
    }
}
