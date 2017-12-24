using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pyratron.Frameworks.Commands.Parser;

namespace ServerShared
{
    public class CommandManager
    {
        private GameServer server;
        private CommandParser parser;

        public CommandManager(GameServer server)
        {
            this.server = server;
            parser = CommandParser.CreateNew(prefix: "/");
        }

        public bool HandleChatMessage(string message)
        {
            return parser.Parse(message);
        }
    }
}
