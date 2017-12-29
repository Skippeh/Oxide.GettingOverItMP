using System.Linq;
using System.Net;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Player;

namespace ServerShared.ChatCommands
{
    [ChatCommand("Unban by name", "unban", "Unban a player with the specified name. The name must partially match the same name that was used when the player was banned.")]
    [RequireAuth(AccessLevel.Moderator)]
    public class UnbanNameCommand : ChatCommand
    {
        [CommandArgument("Player name")]
        public string PlayerName { get; set; }

        public override void Handle(NetPlayer caller, string[] args)
        {
            var bans = Server.FindBansByName(PlayerName).ToList();

            if (bans.Count == 0)
            {
                caller.SendChatMessage("Couldn't find any bans starting with this name.");
                return;
            }

            if (bans.Count > 1)
            {
                caller.SendChatMessage("Found multiple bans starting with this name.");
                return;
            }

            PlayerBan ban = bans.First();
            Server.RemoveBan(ban);
            caller.SendChatMessage($"Unbanned {ban.ReferenceName} ({ban.GetIdentifier()}.", SharedConstants.ColorGreen);
        }
    }

    [ChatCommand("Unban by steam id", "unbanid", "Unban a steam id.")]
    [RequireAuth(AccessLevel.Moderator)]
    public class UnbanSteamIdCommand : ChatCommand
    {
        [CommandArgument("Steam id")]
        public ulong SteamId { get; set; }

        public override void Handle(NetPlayer caller, string[] args)
        {
            if (!Server.UnbanSteamId(SteamId))
            {
                caller.SendChatMessage("Could not find a ban matching the specified steam id. Make sure you enter the steamID64 format.", SharedConstants.ColorRed);
                return;
            }

            caller.SendChatMessage("Unbanned the specified steam id.", SharedConstants.ColorGreen);
        }
    }

    [ChatCommand("Unban by IP", "unbanip", "Unban an IP.")]
    [RequireAuth(AccessLevel.Moderator)]
    public class UnbanIpCommand : ChatCommand
    {
        [CommandArgument("IP")]
        public string IP { get; set; }

        public override void Handle(NetPlayer caller, string[] args)
        {
            if (!IPAddress.TryParse(IP, out var ipAddress))
            {
                caller.SendChatMessage("The specified IP is not valid.", SharedConstants.ColorRed);
                return;
            }

            if (!Server.UnbanIp(ipAddress))
            {
                caller.SendChatMessage("Could not find a ban matching the specified ip.", SharedConstants.ColorRed);
                return;
            }

            caller.SendChatMessage($"Unbanned the specified IP.", SharedConstants.ColorGreen);
        }
    }
}
