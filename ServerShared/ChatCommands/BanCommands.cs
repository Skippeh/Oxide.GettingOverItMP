using System;
using System.Linq;
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

            Server.BanPlayer(banTarget, Reason, Minutes != 0 ? DateTime.UtcNow.AddMinutes(Minutes) : (DateTime?)null);
            AnnounceBan(banTarget);
        }

        private void AnnounceBan(NetPlayer banTarget)
        {
            string suffix;

            if (Minutes == 1)
                suffix = $"for {Minutes} minute";
            else if (Minutes > 1)
                suffix = $"for {Minutes} minutes";
            else
                suffix = "permanently";

            Server.BroadcastChatMessage($"{banTarget.Name} was banned {suffix}.", SharedConstants.ColorBlue);
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
    }
}
