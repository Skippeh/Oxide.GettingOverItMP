using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;
using ServerShared.Player;
using UnityEngine;

namespace ServerShared.Commands.ChatCommands
{
    [Command("Set goldness", "goldness", "Set yours or someone elses pot's goldness.")]
    [RequireAuth(AccessLevel.Moderator)]
    public class SetGoldnessChatCommand : ChatCommand
    {
        [CommandArgument("Goldness")]
        public float Goldness { get; set; }

        [CommandArgument("Player name", optional: true, defaultValue: null)]
        public string PlayerName { get; set; }

        public override void Handle(string[] args)
        {
            Goldness = Mathf.Clamp01(Goldness);
            NetPlayer target = Caller;

            if (PlayerName != null)
            {
                target = Server.FindPlayer(PlayerName, NameSearchOption.StartsWith);

                if (target == null)
                {
                    SendMessage($"Could not find a player with a name starting with '{PlayerName}'.", LogMessageType.Error);
                    return;
                }
            }

            if (target == null)
            {
                SendMessage("Missing player name, if typed in server console a player name needs to be specified.", LogMessageType.Error);
                return;
            }

            if (target.Spectating)
            {
                SendMessage($"Can't set goldness on spectating players.", LogMessageType.Error);
                return;
            }

            target.SetGoldness(Goldness);
        }
    }

    [Command("Set pot color", "potcolor", "Set yours or someone elses pot's color.")]
    [RequireAuth(AccessLevel.Moderator)]
    public class SetPotColorChatCommand : ChatCommand
    {
        [CommandArgument("Red (0-1)")]
        public float R { get; set; }

        [CommandArgument("Green (0-1)")]
        public float G { get; set; }

        [CommandArgument("Blue (0-1)")]
        public float B { get; set; }

        [CommandArgument("Player name", optional: true, defaultValue: null)]
        public string PlayerName { get; set; }

        public override void Handle(string[] args)
        {
            NetPlayer target = Caller;

            if (PlayerName != null)
            {
                target = Server.FindPlayer(PlayerName, NameSearchOption.StartsWith);

                if (target == null)
                {
                    SendMessage($"Could not find a player with a name starting with '{PlayerName}'.", LogMessageType.Error);
                    return;
                }
            }

            if (target == null)
            {
                SendMessage("Missing player name, if typed in server console a player name needs to be specified.", LogMessageType.Error);
                return;
            }

            if (target.Spectating)
            {
                SendMessage($"Can't set pot color on spectating players.", LogMessageType.Error);
                return;
            }

            target.SetPotColor(new Color(R, G, B));
        }
    }
}
