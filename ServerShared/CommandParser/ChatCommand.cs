using ServerShared;
using ServerShared.Player;

namespace Pyratron.Frameworks.Commands.Parser
{
    public abstract class ChatCommand
    {
        public GameServer Server { get; set; }
        public CommandParser Parser { get; set; }

        public abstract void Handle(NetPlayer caller, string[] args);
    }
}
