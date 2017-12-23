using System.Collections.Generic;

namespace ServerShared
{
    public interface IServerConfig
    {
        bool LoadPlayerBans(out List<PlayerBan> bans);
        bool SavePlayerBans(IEnumerable<PlayerBan> bans);
    }
}
