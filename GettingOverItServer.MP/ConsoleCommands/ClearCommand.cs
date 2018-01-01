using Pyratron.Frameworks.Commands.Parser;
using Server;

namespace GettingOverItMP.Server.ConsoleCommands
{
    [Command("Clear", "clear", "Clear the console.")]
    public class ClearCommand : ConsoleCommand
    {
        public override void Handle(string[] args)
        {
            ConsoleManager.Clear();
        }
    }
}
