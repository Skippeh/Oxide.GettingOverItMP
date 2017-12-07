using System;
using System.Threading;
using ServerShared;

namespace GettingOverItMP.Server
{
    public static class Program
    {
        private static GameServer server;

        private static void Main(string[] args)
        {
            server = new GameServer();
            server.Start();

            while (!Console.KeyAvailable)
            {
                server.Update();
                Thread.Sleep(100);
            }
        }
    }
}
