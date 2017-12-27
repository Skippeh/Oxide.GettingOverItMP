using System;
using System.Linq;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Player;

namespace ServerShared.ChatCommands
{
    [ChatCommand("Ban", "ban", "Ban the specified player for an optional amount of time in minutes.")]
    [RequireAuth(AccessLevel.Moderator)]
    public class BanCommand : ChatCommand
    {
        [CommandArgument("Player id")]
        public int PlayerId { get; set; }

        [CommandArgument("Reason")]
        public string Reason { get; set; }

        [CommandArgument("Ban length", optional: true, defaultValue: "0")]
        public int Minutes { get; set; }

        public override void Handle(NetPlayer caller, string[] args)
        {
            var netPlayer = Server.FindPlayer(PlayerId);

            if (netPlayer == null)
            {
                caller.SendChatMessage($"Could not find a player with the id {PlayerId}.", SharedConstants.ColorRed);
                return;
            }

            if (Minutes < 0)
            {
                caller.SendChatMessage("Invalid time specified (needs to be 0 or higher).", SharedConstants.ColorRed);
                return;
            }

            if (netPlayer == caller)
            {
                caller.SendChatMessage("You can't ban yourself.", SharedConstants.ColorRed);
                return;
            }

            if (netPlayer.AccessLevel > caller.AccessLevel)
            {
                caller.SendChatMessage("You can't ban someone with a higher access level than yourself.", SharedConstants.ColorRed);
                return;
            }

            string suffix;

            if (Minutes == 1)
                suffix = $"for {Minutes} minute";
            else if (Minutes > 1)
                suffix = $"for {Minutes} minutes";
            else
                suffix = "permanently";

            Server.BanPlayer(netPlayer, Reason, Minutes != 0 ? DateTime.UtcNow.AddMinutes(Minutes) : (DateTime?) null);
            Server.BroadcastChatMessage($"{netPlayer.Name} was banned {suffix}.", SharedConstants.ColorBlue);
        }
    }
}
