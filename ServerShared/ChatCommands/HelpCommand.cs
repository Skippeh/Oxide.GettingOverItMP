using System.Text;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Player;
using UnityEngine;

namespace ServerShared.ChatCommands
{
    [ChatCommand("Help", "help", "Shows all available commands and their descriptions.")]
    public class HelpCommand : ChatCommand
    {
        public override void Handle(NetPlayer caller, string[] args)
        {
            var builder = new StringBuilder();

            builder.AppendLine("Commands:");

            for (int i = 0; i < Parser.Commands.Count; ++i)
            {
                var command = Parser.Commands[i];

                builder.Append($"{command.Name} [{string.Join(", ", command.Aliases.ToArray())}]: {command.Description}");

                if (i < Parser.Commands.Count - 1)
                    builder.AppendLine();
            }

            caller.SendChatMessage(builder.ToString());
        }
    }
}
