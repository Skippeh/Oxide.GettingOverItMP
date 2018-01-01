using ServerShared;
using ServerShared.Logging;
using ServerShared.Player;

namespace Pyratron.Frameworks.Commands.Parser
{
    public abstract class BaseCommand
    {
        public CommandContext Context { get; set; }
        public GameServer Server { get; set; }
        public CommandParser Parser { get; set; }
        public NetPlayer Caller { get; set; }

        public abstract void Handle(string[] args);

        /// <summary>Sends a message to the command caller.</summary>
        protected void SendMessage(object msg, LogMessageType type)
        {
            switch (Context)
            {
                case CommandContext.Chat:
                    SendMessageChat(msg, type);
                    break;
                case CommandContext.Console:
                    SendMessageConsole(msg, type);
                    break;
            }
        }

        private void SendMessageChat(object msg, LogMessageType type)
        {
            UnityEngine.Color color;

            switch (type)
            {
                case LogMessageType.Debug:
                case LogMessageType.Info:
                    color = SharedConstants.ColorGreen;
                    break;
                case LogMessageType.Warning:
                case LogMessageType.Error:
                case LogMessageType.Exception:
                    color = SharedConstants.ColorRed;
                    break;
                default:
                    color = UnityEngine.Color.white;
                    break;
            }

            Caller.SendChatMessage(msg.ToString(), color);
        }

        private void SendMessageConsole(object msg, LogMessageType type)
        {
            if (Caller != null)
                Caller.SendConsoleMessage(msg.ToString(), type);
            else
                Logger.Log(msg, type);
        }
    }

    public enum CommandContext
    {
        Chat,
        Console
    }
}
