using System;
using System.Threading;
using CommandLineParser.Exceptions;
using ServerShared;

namespace GettingOverItMP.Server
{
    public static class Program
    {
        private static GameServer server;

        private static int Main(string[] args)
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

            server = new GameServer(launchArguments.ServerName, launchArguments.MaxPlayers, launchArguments.Port, false, launchArguments.Private);
            server.Start();

            Console.WriteLine("Press CTRL+Q to stop the server.");

            while (true)
            {
                ConsoleKeyInfo key = default(ConsoleKeyInfo);

                if (Console.KeyAvailable)
                    key = Console.ReadKey(true);

                if (key.Modifiers == ConsoleModifiers.Control && key.Key == ConsoleKey.Q)
                    break;
                
                server.Update();
                Thread.Sleep(1);

                Console.Title = $"{server.Name} | {server.Players.Count}/{server.MaxPlayers}";
            }
            
            server.Stop();
            return 0;
        }
    }
}
