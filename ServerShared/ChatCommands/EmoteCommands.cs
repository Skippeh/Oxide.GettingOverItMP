using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Player;
using UnityEngine;

namespace ServerShared.ChatCommands
{
    [ChatCommand("Shrug", "shrug", "Writes an optional message followed by ¯\\_(ツ)_/¯.")]
    public class ShrugCommand : ChatCommand
    {
        public override void Handle(NetPlayer caller, string[] args)
        {
            string prefix = string.Join(" ", args);
            prefix = prefix.Trim();

            Server.BroadcastChatMessage($"{prefix} ¯\\_(ツ)_/¯", Color.white, caller);
        }
    }

    [ChatCommand("Tableflip", "tableflip", "Writes an optional message followed by (╯°□°）╯︵ ┻━┻.")]
    public class TableflipCommand : ChatCommand
    {
        public override void Handle(NetPlayer caller, string[] args)
        {
            string prefix = string.Join(" ", args);
            prefix = prefix.Trim();

            Server.BroadcastChatMessage($"{prefix} (╯°□°）╯︵ ┻━┻", Color.white, caller);
        }
    }
}
