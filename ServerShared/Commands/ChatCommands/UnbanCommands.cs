using System.Linq;
using System.Net;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;

namespace ServerShared.ChatCommands
{
    [Command("Unban by name", "unban", "Unban a player with the specified name. The name must partially match the same name that was used when the player was banned.")]
    [RequireAuth(AccessLevel.Moderator)]
    public class UnbanNameCommand : ChatCommand
    {
        [CommandArgument("Player name")]
        public string PlayerName { get; set; }

        public override void Handle(string[] args)
        {
            var bans = Server.FindBansByName(PlayerName).ToList();

            if (bans.Count == 0)
            {
                SendMessage("Could not find any bans starting with this name.", LogMessageType.Error);
                return;
            }

            if (bans.Count > 1)
            {
                SendMessage("Found multiple bans starting with this name, be more specific.", LogMessageType.Error);
                return;
            }

            PlayerBan ban = bans.First();
            Server.RemoveBan(ban);
            SendMessage($"Unbanned {ban.ReferenceName} ({ban.GetIdentifier()}).", LogMessageType.Info);
        }
    }

    [Command("Unban by steam id", "unbanid", "Unban a steam id.")]
    [RequireAuth(AccessLevel.Moderator)]
    public class UnbanSteamIdCommand : ChatCommand
    {
        [CommandArgument("Steam id")]
        public ulong SteamId { get; set; }

        public override void Handle(string[] args)
        {
            if (!Server.UnbanSteamId(SteamId))
            {
                SendMessage("Could not find a ban matching the specified steam id. Make sure you enter the steamID64 format.", LogMessageType.Error);
                return;
            }

            SendMessage("Unbanned the specified steam id.", LogMessageType.Info);
        }
    }

    [Command("Unban by IP", "unbanip", "Unban an IP.")]
    [RequireAuth(AccessLevel.Moderator)]
    public class UnbanIpCommand : ChatCommand
    {
        [CommandArgument("IP")]
        public string IP { get; set; }

        public override void Handle(string[] args)
        {
            if (!IPAddress.TryParse(IP, out var ipAddress))
            {
                SendMessage("The specified IP is not valid.", LogMessageType.Error);
                return;
            }

            if (!Server.UnbanIp(ipAddress))
            {
                SendMessage("Could not find a ban matching the specified IP.", LogMessageType.Error);
                return;
            }

            SendMessage("Unbanned the specified IP.", LogMessageType.Info);
        }
    }
}
