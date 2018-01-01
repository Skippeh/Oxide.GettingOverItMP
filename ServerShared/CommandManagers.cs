using System.Reflection;
using Pyratron.Frameworks.Commands.Parser;
using ServerShared.Logging;

namespace ServerShared
{
    public class ChatCommandManager : BaseCommandManager<ChatCommand>
    {
        public ChatCommandManager(GameServer server, Assembly executingAssembly) : base(server, prefix: "/", searchAssemblies: executingAssembly)
        {
        }

        protected override void OnParseError(object sender, string error)
        {
            CurrentCaller?.SendChatMessage(error, SharedConstants.ColorRed);
        }
    }

    public class ConsoleCommandManager : BaseCommandManager<BaseCommand>
    {
        public ConsoleCommandManager(GameServer server, Assembly executingAssembly) : base(server, prefix: "", searchAssemblies: executingAssembly)
        {
        }

        protected override void OnParseError(object sender, string error)
        {
            if (CurrentCaller != null)
                CurrentCaller.SendConsoleMessage(error, LogMessageType.Error);
            else
                Logger.LogError(error);
        }
    }
}
