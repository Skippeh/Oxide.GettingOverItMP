using System;
using Lidgren.Network;

namespace ServerShared
{
    public class ConnectedEventArgs : EventArgs
    {
        public NetConnection Connection { get; set; }
    }
}
