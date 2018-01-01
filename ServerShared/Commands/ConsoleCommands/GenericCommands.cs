using Pyratron.Frameworks.Commands.Parser;
using UnityEngine;

namespace ServerShared.Commands.ConsoleCommands
{
    [Command("Exit", new[] { "exit", "quit" }, "Shutdown the server.")]
    public class ExitCommand : ConsoleCommand
    {
        public override void Handle(string[] args)
        {
            Server.Stop();
        }
    }

    [Command("Say", "say", "Send a chat message to all players.")]
    public class SayCommand : ConsoleCommand
    {
        public override void Handle(string[] args)
        {
            string message = string.Join(" ", args);
            Server.BroadcastChatMessage(message, new Color(0.94f, 0.86f, 0.79f), 0, "Server");
        }
    }
}
