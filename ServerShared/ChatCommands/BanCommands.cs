using System;
using System.Linq;
using System.Net;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Player;

namespace ServerShared.ChatCommands
{
    [ChatCommand("Ban by player id", "banid", "Ban the player with the specified id for an optional amount of time in minutes.")]
    [RequireAuth(AccessLevel.Moderator)]
    public class BanIdCommand : BaseBanCommand
    {
        [CommandArgument("Player id")]
        public int PlayerId { get; set; }

        [CommandArgument("Ban length", optional: true, defaultValue: "0")]
        public int Minutes { get; set; }

        [CommandArgument("Reason", optional: true, defaultValue: null)]
        public string Reason { get; set; }

        public override void Handle(NetPlayer caller, string[] args)
        {
            var banTarget = Server.FindPlayer(PlayerId);

            if (banTarget == null)
            {
                caller.SendChatMessage($"Could not find a player with the id {PlayerId}.", SharedConstants.ColorRed);
                return;
            }

            if (!VerifyArgs(caller, Reason, Minutes))
                return;

            if (banTarget == caller)
            {
                caller.SendChatMessage("You can't ban yourself.", SharedConstants.ColorRed);
                return;
            }

            if (banTarget.AccessLevel > caller.AccessLevel)
            {
                caller.SendChatMessage("You can't ban someone with a higher access level than yourself.", SharedConstants.ColorRed);
                return;
            }

            Server.BanPlayer(banTarget, Reason, Minutes != 0 ? DateTime.UtcNow.AddMinutes(Minutes) : (DateTime?) null);
            AnnounceBan(banTarget, Minutes);
        }
    }

    [ChatCommand("Ban by steam id", "bansteamid", "Ban the specified steam id for an optional amount of time in minutes.")]
    [RequireAuth(AccessLevel.Moderator)]
    public class BanSteamIdCommand : BaseBanCommand
    {
        [CommandArgument("Steam id")]
        public ulong SteamId { get; set; }

        [CommandArgument("Ban length", optional: true, defaultValue: "0")]
        public int Minutes { get; set; }

        [CommandArgument("Reason", optional: true, defaultValue: null)]
        public string Reason { get; set; }

        public override void Handle(NetPlayer caller, string[] args)
        {
            if (!VerifyArgs(caller, Reason, Minutes))
                return;

            if (SteamId  == caller.SteamId)
            {
                caller.SendChatMessage("You can't ban yourself.", SharedConstants.ColorRed);
                return;
            }
            
            // Todo: Check if steam id has higher access level than caller.

            Server.BanSteamId(SteamId, Reason, Minutes != 0 ? DateTime.UtcNow.AddMinutes(Minutes) : (DateTime?) null);
            var player = Server.FindPlayer(SteamId);

            if (player != null)
            {
                Server.KickConnection(player.Peer, DisconnectReason.Banned, Reason);
                AnnounceBan(player, Minutes);
            }
        }
    }

    [ChatCommand("Ban by IP", "banip", "Ban the specified ip for an optional amount of time in minutes.")]
    [RequireAuth(AccessLevel.Admin)]
    public class BanIpCommand : BaseBanCommand
    {
        [CommandArgument("Steam id")]
        public string IP { get; set; }

        [CommandArgument("Ban length", optional: true, defaultValue: "0")]
        public int Minutes { get; set; }

        [CommandArgument("Reason", optional: true, defaultValue: null)]
        public string Reason { get; set; }

        public override void Handle(NetPlayer caller, string[] args)
        {
            if (!VerifyArgs(caller, Reason, Minutes))
                return;

            if (!IPAddress.TryParse(IP, out var ipAddress))
            {
                caller.SendChatMessage("Invalid ip specified.", SharedConstants.ColorRed);
                return;
            }

            Server.BanIp(ipAddress, Reason, Minutes != 0 ? DateTime.UtcNow.AddMinutes(Minutes) : (DateTime?) null);
            var player = Server.FindPlayer(ipAddress);

            if (player != null)
            {
                Server.KickConnection(player.Peer, DisconnectReason.Banned, Reason);
                AnnounceBan(player, Minutes);
            }
        }
    }

    public abstract class BaseBanCommand : ChatCommand
    {
        protected bool VerifyArgs(NetPlayer caller, string reason, int minutes)
        {
            if (minutes < 0)
            {
                caller.SendChatMessage("Invalid time specified (needs to be 0 or higher).", SharedConstants.ColorRed);
                return false;
            }

            return true;
        }

        protected void AnnounceBan(NetPlayer banTarget, int minutes)
        {
            string suffix;

            if (minutes == 1)
                suffix = $"for {minutes} minute";
            else if (minutes > 1)
                suffix = $"for {minutes} minutes";
            else
                suffix = "permanently";

            Server.BroadcastChatMessage($"{banTarget.Name} was banned {suffix}.", SharedConstants.ColorBlue);
        }
    }
}
