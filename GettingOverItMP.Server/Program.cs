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

            Console.WriteLine("Press any key to stop the server.");

            while (!Console.KeyAvailable)
            {
                server.Update();
                Thread.Sleep(33);
            }

            server.Stop();
        }
    }
}
