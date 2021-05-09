using System;
using System.Collections.Generic;
using System.Threading;
using CommandLineParser.Exceptions;
using GettingOverItMP.Server;
using ServerShared;

namespace Server
{
    class Program
    {
        public static GameServer Server { get; private set; }

        private static Queue<string> queuedCommandInputs = new Queue<string>();

        static int Main(string[] args)
        {
            var parser = new CommandLineParser.CommandLineParser();
            var launchArguments = new LaunchArguments();

            try
            {
                parser.ExtractArgumentAttributes(launchArguments);
                parser.ParseCommandLine(args);
            }
            catch (CommandLineException ex)
            {
                Console.WriteLine(ex.Message);
                parser.ShowUsage();
                return 1;
            }

            if (launchArguments.MaxPlayers > SharedConstants.MaxPlayerLimit)
            {
                Console.WriteLine($"Max players exceeded max limit of {SharedConstants.MaxPlayerLimit}. Value was adjusted.");
                launchArguments.MaxPlayers = SharedConstants.MaxPlayerLimit;
            }

            if (launchArguments.MaxPlayers < 1)
            {
                Console.WriteLine("Player limit can't be lower than 1.");
                parser.ShowUsage();
                return 1;
            }

            ConsoleManager.Initialize();
            ConsoleManager.OnInput += cmd => queuedCommandInputs.Enqueue(cmd);

            Server = new GameServer(launchArguments.ServerName, launchArguments.MaxPlayers, launchArguments.Port, false, launchArguments.Private, !launchArguments.NoSteam, "config");
            Server.Start();
            
            while (Server.Running)
            {
                while (queuedCommandInputs.Count > 0)
                    Server.ConsoleCommands.HandleMessage(null, queuedCommandInputs.Dequeue());

                if (!Server.Running)
                    continue;

                Server.Update();
                
                Console.Title = $"{Server.Name} | {Server.Players.Count}/{Server.MaxPlayers}";
                Thread.Sleep(1);
            }
            
            ConsoleManager.Destroy();
            return 0;
        }
    }
}
