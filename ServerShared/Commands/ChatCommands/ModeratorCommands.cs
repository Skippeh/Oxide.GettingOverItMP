using System.Net;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared.Commands.ChatCommands
{
    [Command("Set access level by steam id", "accesslevel", "Set the access level by steam id. Allowed values are 'player', 'moderator', and 'admin'.")]
    [RequireAuth(AccessLevel.Admin)]
    public class SetAccessLevelBySteamId : ChatCommand
    {
        [CommandArgument("Steam id")]
        public ulong SteamId { get; set; }

        [CommandArgument("Access level")]
        public AccessLevel AccessLevel { get; set; }

        public override void Handle(string[] args)
        {
            if (AccessLevel > AccessLevel.Admin)
            {
                SendMessage("Invalid access level specified.", LogMessageType.Error);
                return;
            }

            Server.SetAccessLevel(SteamId, AccessLevel);
            SendMessage($"{SteamId} access level set to {AccessLevel}.", LogMessageType.Info);
        }
    }

    [Command("Set access level by IP", "accesslevelip", "Set the access level by IP. Allowed values are 'player', 'moderator', and 'admin'.")]
    [RequireAuth(AccessLevel.Admin)]
    public class SetAccessLevelByIp : ChatCommand
    {
        [CommandArgument("IP")]
        public string IP { get; set; }

        [CommandArgument("Access level")]
        public AccessLevel AccessLevel { get; set; }

        public override void Handle(string[] args)
        {
            if (AccessLevel > AccessLevel.Admin)
            {
                SendMessage("Invalid access level specified.", LogMessageType.Error);
                return;
            }

            if (!IPAddress.TryParse(IP, out var ipAddress))
            {
                SendMessage("Invalid IP specified.", LogMessageType.Error);
                return;
            }

            Server.SetAccessLevel(ipAddress, AccessLevel);
            SendMessage($"{ipAddress} access level set to {AccessLevel}.", LogMessageType.Info);
        }
    }
}
