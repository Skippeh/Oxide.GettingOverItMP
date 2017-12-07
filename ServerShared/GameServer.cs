using System;
using LiteNetLib;

namespace ServerShared
{
    public class GameServer
    {
        private EventBasedNetListener listener;
        private NetManager server;

        public GameServer(int maxConnections)
        {
            if (maxConnections <= 0)
                throw new ArgumentException("Max connections needs to be > 0.");

            server = new NetManager(listener, maxConnections, "GOIMP");
        }

        public void Start()
        {

        }

        public void Update()
        {

        }
    }
}
