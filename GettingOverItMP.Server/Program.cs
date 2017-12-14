using System;
using System.Threading;
using ServerShared;

namespace GettingOverItMP.Server
{
    public static class Program
    {
        public static int MaxConnections = 100;
        public static int Port = SharedConstants.DefaultPort;

        private static GameServer server;

        private static void Main(string[] args)
        {
            server = new GameServer(MaxConnections, Port, false);
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
            }
            
            server.Stop();
        }
    }
}
