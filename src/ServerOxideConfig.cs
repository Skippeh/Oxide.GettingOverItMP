using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.GettingOverIt;
using ServerShared;

namespace Oxide.GettingOverItMP
{
    public class ServerOxideConfig : IServerConfig
    {
        private DynamicConfigFile config => MPCore.Config;
        
        public bool LoadPlayerBans(out List<PlayerBan> bans)
        {
            try
            {
                bans = config.ReadObject<List<PlayerBan>>("goimp/bans.json");
                return true;
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogDebug($"Failed to load player bans: {ex.Message}");
                bans = null;
                return false;
            }
        }

        public bool SavePlayerBans(IEnumerable<PlayerBan> bans)
        {
            try
            {
                config.WriteObject(bans.ToList(), false, "goimp/bans.json");
                return true;
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogError($"Failed to save player bans: {ex.Message}");
                return false;
            }
        }
    }
}
