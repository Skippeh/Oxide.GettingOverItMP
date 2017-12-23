using Lidgren.Network;
using ServerShared.Player;

namespace ServerShared
{
    public class PendingConnection
    {
        public readonly NetConnection Client;
        public readonly ulong SteamId;
        public string PlayerName;
        public PlayerMove Movement;

        public PendingConnection(NetConnection client, ulong steamId, string playerName, PlayerMove movement)
        {
            Client = client;
            SteamId = steamId;
            PlayerName = playerName;
            Movement = movement;
        }
    }
}
