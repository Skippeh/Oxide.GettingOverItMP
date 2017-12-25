using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Player;
using UnityEngine;

namespace ServerShared.ChatCommands
{
    [ChatCommand("Shrug", "shrug", "¯\\_(ツ)_/¯")]
    public class ShrugCommand : ChatCommand
    {
        public override void Handle(NetPlayer caller, string[] args)
        {
            string prefix = string.Join(" ", args);
            prefix = prefix.Trim();

            Server.BroadcastChatMessage($"{prefix} ¯\\_(ツ)_/¯", Color.white, caller);
        }
    }

    [ChatCommand("Tableflip", "tableflip", "(╯°□°）╯︵ ┻━┻")]
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
