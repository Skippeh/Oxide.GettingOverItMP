using System.Linq;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Player;

namespace ServerShared.ChatCommands
{
    [ChatCommand("Spectate by name", "spectate", "Start spectating another player by their name.")]
    public class SpectateCommand : ChatCommand
    {
        [CommandArgument("Player name")]
        public string PlayerName { get; set; }
        
        public override void Handle(NetPlayer caller, string[] args)
        {
            NetPlayer target;

            var players = Server.Players.Values.Where(plr => !plr.Spectating && plr.Name.ToLower().Contains(PlayerName.ToLower())).ToList();

            if (players.Count == 0)
            {
                caller.SendChatMessage("There is no player with this name.", SharedConstants.ColorRed);
                return;
            }

            if (players.Count > 1)
            {
                caller.SendChatMessage("Found more than 1 player with this name. Try be more specific or use /spectateid and specify their id instead.", SharedConstants.ColorRed);
                return;
            }

            target = players.First();

            if (target == caller)
            {
                caller.SendChatMessage("You can't spectate yourself dummy.", SharedConstants.ColorRed);
                return;
            }

            caller.Spectate(target);
        }
    }

    [ChatCommand("Spectate by id", "spectateid", "Start spectating another player by their id.")]
    public class SpectateIdCommand : ChatCommand
    {
        [CommandArgument("Player name")]
        public int PlayerId { get; set; }

        public override void Handle(NetPlayer caller, string[] args)
        {
            NetPlayer target = Server.Players.Values.FirstOrDefault(player => !player.Spectating && player.Id == PlayerId);

            if (target == null)
            {
                caller.SendChatMessage("There is no player with this id.", SharedConstants.ColorRed);
                return;
            }

            if (target == caller)
            {
                caller.SendChatMessage("You can't spectate yourself dummy.", SharedConstants.ColorRed);
                return;
            }

            caller.Spectate(target);
        }
    }
}
