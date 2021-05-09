using System;
using System.IO;
using System.Net;
using Oxide.Core;
using Oxide.GettingOverIt;
using ServerShared;
using ServerShared.Logging;
using ServerShared.Player;
using Steamworks;

namespace Oxide.GettingOverItMP.Networking
{
    public static class ListenServer
    {
        public static GameServer Server { get; private set; }
        public static bool Running => Server != null;

        public static void Start(string name, int maxPlayers, int port, bool isPrivate, bool requireSteamAuth)
        {
            if (Running)
                throw new InvalidOperationException("The server is already running.");

            Logger.LogMessageReceived += OnLogMessageReceived;

            Server = new GameServer(name, maxPlayers, port, true, isPrivate, requireSteamAuth, Path.Combine(Interface.Oxide.ConfigDirectory, "goimp"));
            Server.Start();

            if (SteamClient.IsValid)
                Server.SetAccessLevel(SteamClient.SteamId, AccessLevel.Admin);
            else
                Server.SetAccessLevel(IPAddress.Parse("127.0.0.1"), AccessLevel.Admin);
        }

        private static void OnLogMessageReceived(LogMessageReceivedEventArgs args)
        {
            switch (args.Type)
            {
                case LogMessageType.Info:
                    Interface.Oxide.LogInfo(args.Message.ToString());
                    break;
                case LogMessageType.Debug:
                    Interface.Oxide.LogDebug(args.Message.ToString());
                    break;
                case LogMessageType.Warning:
                    Interface.Oxide.LogWarning(args.Message.ToString());
                    break;
                case LogMessageType.Error:
                    Interface.Oxide.LogError(args.Message.ToString());
                    break;
                case LogMessageType.Exception:
                    Interface.Oxide.LogException(args.Message.ToString(), args.Exception);
                    break;
            }
        }

        public static void Stop()
        {
            if (!Running)
                throw new InvalidOperationException("The server is not running.");

            Server.Stop();
            Server = null;
            Logger.LogMessageReceived -= OnLogMessageReceived;
        }

        public static void Update()
        {
            if (Running)
                Server.Update();
        }
    }
}
