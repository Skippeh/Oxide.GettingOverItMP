using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Player;
using UnityEngine;

namespace ServerShared.ChatCommands
{
    [Command("Shrug", "shrug", "Writes an optional message followed by ¯\\_(ツ)_/¯.")]
    [RequireCaller]
    public class ShrugCommand : ChatCommand
    {
        public override void Handle(string[] args)
        {
            string prefix = string.Join(" ", args);
            prefix = prefix.Trim();
            
            Server.BroadcastChatMessage($"{prefix} ¯\\_(ツ)_/¯", Color.white, Caller);
        }
    }

    [Command("Tableflip", "tableflip", "Writes an optional message followed by (╯°□°）╯︵ ┻━┻.")]
    [RequireCaller]
    public class TableflipCommand : ChatCommand
    {
        public override void Handle(string[] args)
        {
            string prefix = string.Join(" ", args);
            prefix = prefix.Trim();

            Server.BroadcastChatMessage($"{prefix} (╯°□°）╯︵ ┻━┻", Color.white, Caller);
        }
    }
}
