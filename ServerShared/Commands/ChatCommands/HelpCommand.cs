using System.Text;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;

namespace ServerShared.ChatCommands
{
    [Command("Help", "help", "Shows all available commands and their descriptions.")]
    public class HelpCommand : ChatCommand
    {
        public override void Handle(string[] args)
        {
            var builder = new StringBuilder();

            builder.AppendLine("Commands:");

            for (int i = 0; i < Parser.Commands.Count; ++i)
            {
                var command = Parser.Commands[i];

                if (Caller != null && command.AccessLevel > (int) Caller.AccessLevel)
                    continue;

                if (Caller == null && command.RequireCaller)
                    continue;

                builder.Append($"{command.Name} [{string.Join(", ", command.Aliases.ToArray())}]: {command.Description}");

                if (i < Parser.Commands.Count - 1)
                    builder.AppendLine();
            }

            SendMessage(builder.ToString(), LogMessageType.Info);
        }
    }
}
