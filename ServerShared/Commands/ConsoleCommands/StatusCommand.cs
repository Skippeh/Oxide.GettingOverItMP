using System.Linq;
using System.Text;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;

namespace ServerShared.Commands.ConsoleCommands
{
    [Command("Status", "status", "Show server status and connected players.")]
    public class StatusCommand : ConsoleCommand
    {
        public override void Handle(string[] args)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Host: {Server.Peer.Configuration.LocalAddress}:{Server.Port}");
            builder.AppendLine($"Server name: {Server.Name}");
            builder.AppendLine($"Connected players: {Server.Players.Count}/{Server.MaxPlayers}");

            foreach (var player in Server.Players.Values)
            {
                builder.AppendLine($"- {player.Name} ({player.Identity})");
            }

            SendMessage(builder.ToString().Trim(), LogMessageType.Info);
        }
    }
}
