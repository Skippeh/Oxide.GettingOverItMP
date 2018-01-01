using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared.Commands.ChatCommands
{
    [Command("Set access level by steam id", "accesslevel", "Set the access level by steam id. Allowed values are 'moderator' and 'admin'.")]
    [RequireAuth(AccessLevel.Admin)]
    public class SetAccessLevel : ChatCommand
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
}
